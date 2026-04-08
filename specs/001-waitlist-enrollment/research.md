# Research: Inscripción en Lista de Espera

**Feature**: 001-waitlist-enrollment  
**Date**: 2026-04-07  
**Input**: [plan.md](plan.md) Technical Context + user arguments

## Research Tasks

### R1: Partial unique index con EF Core en PostgreSQL

**Decision**: Usar `HasFilter()` en la configuración de `OnModelCreating` para crear un partial unique index sobre `(event_id, buyer_email) WHERE status = 'active'`.

**Rationale**: EF Core soporta nativamente índices filtrados con el proveedor de Npgsql. Esto se mapea directamente a `CREATE UNIQUE INDEX ... WHERE status = 'active'` en PostgreSQL, que es exactamente la restricción necesaria (FR-004). La alternativa de crear el índice manualmente en SQL y no reflejarlo en EF Core fue rechazada porque rompería la coherencia entre el modelo de EF Core y el esquema real.

**Alternativas consideradas**:
- Crear el índice solo en `schema.sql` sin reflejar en EF Core → rechazada: el DbContext no tendría conocimiento de la restricción, y EF Core podría generar migraciones inconsistentes.
- Usar una restricción UNIQUE con columna `is_active` booleana → rechazada: el enum `WaitlistEntryStatus` ya modela los estados; agregar un booleano duplicaría información.

### R2: Enum PostgreSQL para WaitlistEntryStatus

**Decision**: Crear un nuevo tipo enum `waitlist_entry_status` en PostgreSQL con valores `'active'`, `'consumed'`, `'expired'`. El enum C# será `WaitlistEntryStatus` con PascalCase y atributos `[PgName]`, siguiendo la convención existente de `TicketStatus` y `PaymentStatus`.

**Rationale**: El proyecto ya usa enums PostgreSQL para `ticket_status` y `payment_status` con el patrón `[PgName("lowercase")]` en C#. Seguir la misma convención mantiene consistencia y permite que el partial unique index filtre directamente sobre el valor del enum.

**Alternativas consideradas**:
- Usar un `VARCHAR` para el estado → rechazada: los enums PostgreSQL son más estrictos, ofrecen validación a nivel de BD, y el proyecto ya usa este patrón.
- Reutilizar un enum existente con valores compartidos → rechazada: WaitlistEntry tiene estados diferentes a Ticket y Payment; mezclar dominios violaría SRP.

### R3: Testcontainers para pruebas de integración del repositorio

**Decision**: Usar `Testcontainers.PostgreSql` para las pruebas de integración del `WaitlistEntryRepository`. La clase de test implementará `IAsyncLifetime` para setup y teardown del contenedor. El contenedor ejecutará `schema.sql` como script de inicialización para crear las tablas y el partial unique index.

**Rationale**: La constitución exige Testcontainers para pruebas de integración (Principio IV). El proyecto existente no tiene pruebas Testcontainers en `CrudService.Infrastructure.Tests` (usa mocks y pruebas de componente), pero el test case TC-HU1-05 requiere explícitamente una "base de datos real disponible con el índice parcial aplicado".

**Alternativas consideradas**:
- Usar InMemoryDatabase de EF Core → rechazada: no soporta índices parciales ni enums PostgreSQL; la prueba no validaría la restricción real.
- Usar SQLite en memoria → rechazada: no soporta enums PostgreSQL ni `CREATE INDEX ... WHERE`.

### R4: Manejo del conflicto de unicidad (duplicados concurrentes)

**Decision**: El handler verifica primero en la capa de aplicación si existe una inscripción activa (defensive check). Si dos solicitudes concurrentes pasan la verificación simultáneamente, el partial unique index lanza `PostgresException` con código `23505` (unique_violation). El repositorio atrapa esta excepción y lanza `DuplicateWaitlistEntryException`, que el controller mapea a HTTP 409.

**Rationale**: La doble validación (aplicación + BD) es explícitamente requerida por FR-004. La validación en aplicación cubre el caso común (fail fast sin roundtrip extra a BD), y la restricción de BD cubre la race condition.

**Alternativas consideradas**:
- Confiar solo en la validación de aplicación → rechazada: vulnerable a race conditions bajo concurrencia.
- Confiar solo en la restricción de BD → rechazada: la respuesta de error sería genérica; no podría distinguir entre "duplicado" y otros errores de BD sin parsear el código de excepción.

### R5: Comparación de fecha para cierre de lista de espera

**Decision**: Comparar `DateTime.UtcNow` con `Event.StartsAt`. Si `StartsAt <= DateTime.UtcNow`, la lista está cerrada. Esto alinea con el TC-HU1-03 que dice "La fecha del evento ya fue alcanzada" y el escenario de aceptación US3 que dice "un evento cuya fecha es exactamente hoy → rechazado".

**Rationale**: `StartsAt` es `TIMESTAMPTZ` en PostgreSQL, por lo que incluye hora. Comparar con `DateTime.UtcNow` es preciso a nivel de instante. La comparación `<=` asegura que el momento exacto del evento también cierra la lista.

**Alternativas consideradas**:
- Comparar solo la fecha (sin hora) → rechazada: `StartsAt` es `TIMESTAMPTZ` con hora; truncar perdería precisión.
- Agregar un campo `WaitlistClosesAt` separado → rechazada: la spec define que la lista cierra "cuando se alcanza la fecha del evento"; no hay requisito de cierre anticipado.

### R6: Mapeo del status en DTOs de respuesta

**Decision**: El `WaitlistEntryDto` devolverá el status como `string` (no enum), siguiendo la convención existente de `TicketDto.Status`. El mapeo se hace con `status.ToString().ToLowerInvariant()` para mantener consistencia con los valores PostgreSQL en minúsculas.

**Rationale**: El frontend del proyecto consume estados como strings. Usar el mismo patrón evita inconsistencias.

**Alternativas consideradas**:
- Devolver el enum directamente → rechazada: el serializador JSON por defecto generaría valores numéricos, lo cual sería confuso para el frontend.
