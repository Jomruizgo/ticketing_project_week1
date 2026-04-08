# Research: Consulta de Estado de Lista de Espera

**Feature**: 002-waitlist-status-query  
**Date**: 2026-04-07  
**Input**: [plan.md](plan.md) Technical Context + user arguments

## Research Tasks

### R1: Estrategia para la entidad WaitlistOpportunity (no existente aún en BD)

**Decision**: Definir la entidad `WaitlistOpportunity` en Domain y su interfaz de repositorio `IWaitlistOpportunityRepository`, pero implementar el adaptador de Infrastructure de forma que si la tabla `waitlist_opportunities` no existe en BD, el repositorio devuelva `null` sin lanzar excepción. En la práctica inmediata, el handler consulta el repositorio y si no existe oportunidad, devuelve `opportunity: null` en la respuesta.

**Rationale**: La spec asume que la entidad `WaitlistOpportunity` será creada por la feature de asignación de oportunidades (Feature1). Definir el puerto ahora permite que el handler tenga la lógica completa de proyección de los cuatro estados sin necesidad de modificaciones futuras. El adaptador de Infrastructure puede verificar si el DbSet tiene tabla asociada o simplemente devolver null si no encuentra registros.

**Alternativas consideradas**:
- No definir `WaitlistOpportunity` hasta Feature1 → rechazada: el handler necesita la interfaz del repositorio para compilar y testear; no tenerla obligaría a reescribir el handler cuando se implemente Feature1.
- Crear la tabla `waitlist_opportunities` en BD ahora → rechazada: agrega schema que no tiene escritor aún; viola el principio de no agregar complejidad prematura.

### R2: Extensión de IWaitlistEntryRepository vs. nueva interfaz de consulta

**Decision**: Agregar un método `FindActiveByEventAndEmailAsync(long eventId, string buyerEmail)` a la interfaz existente `IWaitlistEntryRepository`. Este método devuelve la inscripción activa más reciente o `null`.

**Rationale**: El repositorio ya sigue el patrón de interfaz segregada con métodos específicos (`ExistsActiveAsync`, `AddAsync`). Agregar un método de lectura que retorne la entidad completa (no solo un booleano) es una extensión natural que respeta ISP — el handler de consulta necesita la entidad, no solo su existencia. Crear una interfaz de lectura separada (ej. `IWaitlistEntryReadRepository`) agregaría complejidad innecesaria para un solo método adicional.

**Alternativas consideradas**:
- Nueva interfaz `IWaitlistEntryReadRepository` → rechazada: over-engineering para un solo método; violaría KISS. Si en el futuro se necesitan muchas consultas de lectura, se puede segregar la interfaz en ese momento.
- Usar `IQueryable` expuesto desde Infrastructure → rechazada: violaría la regla de dependencia (filtraría EF Core hacia Application).

### R3: Cálculo de minutos restantes — floor vs. ceiling vs. truncate

**Decision**: Usar truncamiento a entero (Math.Max(0, (int)(expiresAt - DateTime.UtcNow).TotalMinutes)). Si el tiempo restante es negativo (oportunidad ya expirada), devolver 0.

**Rationale**: La spec dice "los minutos restantes de validez calculados dinámicamente" (FR-006) y "precisos con un margen de error menor a 1 minuto" (SC-003). Truncar a entero hacia abajo es conservador: nunca sobrestima el tiempo restante, lo cual es preferible desde la perspectiva del comprador (mejor que piense que tiene 11 minutos cuando en realidad tiene 11:45, a que piense que tiene 12). `Math.Max(0, ...)` evita valores negativos si la consulta ocurre justo cuando la oportunidad expira.

**Alternativas consideradas**:
- Ceiling (redondear hacia arriba) → rechazada: podría generar una percepción de más tiempo del real, causando frustración si la oportunidad expira antes de lo esperado.
- Decimal con precisión de segundos → rechazada: la spec pide "minutos restantes" como entero; el frontend espera un número entero para mostrar.

### R4: Manejo del estado "expired" vs. cálculo dinámico de expiración

**Decision**: El handler lee el campo `Status` de la oportunidad tal como está persistido en BD. NO recalcula si la oportunidad debería haber expirado. Si `Status = Active` pero `ExpiresAt < DateTime.UtcNow`, el handler muestra `remainingMinutes: 0` pero mantiene el status reportado como "active" — el cambio de estado a "expired" es responsabilidad de otro flujo (worker de expiración).

**Rationale**: La spec dice que la operación es de solo lectura (FR-011, SC-005). Si el handler mutara el estado de la oportunidad de "active" a "expired" al detectar que ExpiresAt pasó, estaría violando ambos requisitos. La consistencia eventual es un principio del sistema (Principio II de la constitución). El edge case de la spec dice: "la oportunidad expira exactamente en el instante de la consulta → muestra según el estado ya persistido".

**Alternativas consideradas**:
- Recalcular y actualizar el estado en la consulta → rechazada: viola FR-011 y SC-005 (solo lectura); además introduce race conditions con el worker que gestiona expiración.
- Devolver un status derivado ("technically expired") → rechazada: la spec define exactamente cuatro estados; inventar uno nuevo rompe el contrato.

### R5: Estructura del WaitlistStatusResponse DTO

**Decision**: Crear un DTO `WaitlistStatusResponse` con dos propiedades: `Entry` (WaitlistEntryDto, no nulo) y `Opportunity` (WaitlistOpportunityDto, nullable). El `WaitlistOpportunityDto` incluye: `Id`, `TicketId`, `Status` (string lowercase), `ActivatedAt`, `ExpiresAt`, `RemainingMinutes` (int). La respuesta serializada incluye `opportunity: null` cuando no hay oportunidad.

**Rationale**: Alinea con el contrato de API documentado en `docs/Features/Feature1/API_CONTRACTS.md` para el endpoint `GET /api/waitlist/entries`. Reutiliza `WaitlistEntryDto` del feature 001 sin modificaciones. El `WaitlistOpportunityDto` es un DTO nuevo, plano, sin lógica — solo proyección.

**Alternativas consideradas**:
- Un único DTO aplanado con todos los campos → rechazada: mezcla datos de dos entidades distintas, dificulta la lectura del frontend y rompe la estructura del contrato ya documentado.
- Devolver solo el `WaitlistEntryDto` con un campo extra para oportunidad embebida → rechazada: modificar el DTO existente podría romper consumidores del endpoint POST que esperan el DTO original.

### R6: Validación del email en consulta GET

**Decision**: Aplicar la misma validación de formato de email que el POST (RFC 5321 simplificado) antes de consultar la BD. Normalizar a minúsculas antes de comparar (consistente con la clarificación de 001-waitlist-enrollment).

**Rationale**: FR-005 requiere validar formato de email y devolver 400 si es inválido. Normalizar a minúsculas antes de buscar asegura que la consulta coincida con inscripciones guardadas con el email normalizado (según la clarificación de 001). Sin normalización, un comprador que se inscribió como `user@example.com` no se encontraría si consulta como `User@EXAMPLE.com`.

**Alternativas consideradas**:
- No validar email en GET (solo validar en POST) → rechazada: FR-005 es explícito; además, pasar basura a la BD sin validar es mala práctica.
- Validación solo de presencia, sin formato → rechazada: abre la puerta a consultas con parámetros malformados que llegarían a la BD innecesariamente.
