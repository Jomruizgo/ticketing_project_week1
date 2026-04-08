# Feature Specification: Inscripción en Lista de Espera

**Feature Branch**: `001-waitlist-enrollment`  
**Created**: 2026-04-07  
**Status**: Draft  
**Input**: User description: "Construye una feature de inscripción para un sistema de lista de espera dentro de una plataforma de venta de entradas existente."

## Clarifications

### Session 2026-04-07

- Q: ¿La comparación de email para unicidad es case-sensitive o case-insensitive? → A: Normalizar a minúsculas antes de persistir y comparar (case-insensitive).
- Q: ¿El cierre de la lista de espera se determina por fecha del día o por fecha y hora exacta del evento? → A: Comparar contra fecha y hora exacta del evento (datetime preciso).
- Q: ¿La lista de espera tiene un tamaño máximo por evento? → A: Límite global fijo para todos los eventos, definido como constante de configuración.
- Q: ¿La respuesta 409 por duplicado incluye datos de la inscripción existente o solo mensaje de error? → A: Solo mensaje de error descriptivo, sin exponer datos de la inscripción existente.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Inscripción exitosa en lista de espera (Priority: P1)

Un comprador visita un evento que no tiene entradas disponibles. Proporciona su correo electrónico para registrarse en la lista de espera. El sistema crea una inscripción con estado "active" y confirma al comprador que ha sido registrado.

**Why this priority**: Es el flujo principal de la feature — sin inscripción exitosa no hay lista de espera. Representa el valor central para el comprador y el negocio.

**Independent Test**: Se puede verificar enviando una solicitud con un evento válido sin entradas y un correo electrónico nuevo; el sistema devuelve la inscripción creada con estado "active".

**Acceptance Scenarios**:

1. **Given** un evento existente sin entradas disponibles cuya fecha aún no ha pasado, **When** un comprador envía su correo electrónico para inscribirse, **Then** el sistema crea una inscripción con estado "active" y responde con los datos de la inscripción (código 201).
2. **Given** un comprador cuya inscripción anterior en el mismo evento tiene estado "consumed" o "expired", **When** envía una nueva solicitud de inscripción, **Then** el sistema crea una nueva inscripción "active" (código 201).

---

### User Story 2 — Rechazo de inscripción duplicada (Priority: P2)

Un comprador que ya tiene una inscripción activa en un evento intenta inscribirse de nuevo en el mismo evento. El sistema rechaza la solicitud indicando que ya existe una inscripción activa.

**Why this priority**: Garantiza la integridad de datos y la regla de unicidad del negocio. Sin esta validación, la lista de espera tendría duplicados que corromperían la lógica de asignación futura.

**Independent Test**: Se puede verificar creando primero una inscripción activa y luego enviando una segunda solicitud idéntica; el sistema responde con error de conflicto.

**Acceptance Scenarios**:

1. **Given** un comprador que ya tiene una inscripción activa para un evento, **When** envía otra solicitud de inscripción para el mismo evento, **Then** el sistema rechaza la solicitud con código 409 y un mensaje indicando duplicidad.
2. **Given** un comprador con inscripción activa en el evento A, **When** envía una solicitud de inscripción para el evento B, **Then** el sistema crea la inscripción normalmente (código 201), porque la unicidad es por par evento-comprador.

---

### User Story 3 — Rechazo por lista de espera cerrada (Priority: P3)

Un comprador intenta inscribirse en la lista de espera de un evento cuya fecha ya pasó. El sistema rechaza la solicitud indicando que la lista de espera está cerrada.

**Why this priority**: Protege la coherencia temporal del sistema. Sin este control, se aceptarían inscripciones para eventos ya realizados, generando expectativas imposibles de cumplir.

**Independent Test**: Se puede verificar enviando una solicitud de inscripción para un evento con fecha anterior a la actual; el sistema responde con error de validación.

**Acceptance Scenarios**:

1. **Given** un evento cuya fecha y hora ya han pasado, **When** un comprador envía una solicitud de inscripción, **Then** el sistema rechaza la solicitud con código 422 y un mensaje indicando que la lista de espera está cerrada.
2. **Given** un evento programado para hoy a las 20:00 y la hora actual es 20:01, **When** un comprador envía una solicitud de inscripción, **Then** el sistema rechaza la solicitud con código 422, ya que la hora exacta del evento se ha alcanzado.
3. **Given** un evento programado para hoy a las 20:00 y la hora actual es 19:59, **When** un comprador envía una solicitud de inscripción, **Then** el sistema acepta la inscripción (código 201), porque la hora del evento aún no ha llegado.

---

### Edge Cases

- ¿Qué sucede si el `eventId` proporcionado no corresponde a ningún evento existente? El sistema responde con código 404.
- ¿Qué sucede si el correo electrónico tiene formato inválido? El sistema responde con código 400 indicando error de validación.
- ¿Qué sucede si el `eventId` o `buyerEmail` no se envían en el cuerpo de la solicitud? El sistema responde con código 400 indicando campos obligatorios faltantes.
- ¿Qué sucede si dos compradores envían solicitudes de inscripción simultáneamente con el mismo correo y evento? El partial unique index en la base de datos garantiza que solo una se persiste; la segunda recibe código 409.
- ¿Qué sucede si el evento aún tiene entradas disponibles? La inscripción en lista de espera se acepta igualmente — la verificación de disponibilidad de entradas pertenece al flujo de compra, no al de lista de espera.
- ¿Qué sucede si la lista de espera alcanza el límite global configurado? El sistema rechaza la inscripción con código 422 indicando que la lista de espera está llena.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema DEBE permitir que un comprador se inscriba en la lista de espera de un evento proporcionando su correo electrónico.
- **FR-002**: El sistema DEBE crear la inscripción con estado "active" al registrar exitosamente al comprador.
- **FR-003**: El sistema DEBE rechazar con código 409 cualquier inscripción cuando ya existe una inscripción activa del mismo comprador (mismo correo) para el mismo evento. El cuerpo de la respuesta contiene únicamente un mensaje de error descriptivo, sin incluir datos de la inscripción existente.
- **FR-004**: La unicidad de inscripción activa por par evento-comprador DEBE aplicarse tanto a nivel de lógica de aplicación como a nivel de base de datos mediante partial unique index. La comparación de email es case-insensitive: el sistema DEBE normalizar el correo electrónico a minúsculas antes de persistir y antes de comparar.
- **FR-005**: El sistema DEBE rechazar con código 422 cualquier inscripción cuando la fecha y hora exacta del evento ya se ha alcanzado (lista de espera cerrada). La comparación se realiza contra el campo datetime preciso del evento, no solo el componente de fecha.
- **FR-006**: El sistema DEBE permitir la reinscripción de un comprador cuya inscripción anterior en el mismo evento tiene estado "consumed" o "expired".
- **FR-007**: La inscripción en lista de espera NO DEBE reservar ninguna entrada; solo registra la intención del comprador.
- **FR-008**: El comprador se identifica únicamente por correo electrónico; no existe autenticación.
- **FR-009**: El sistema DEBE validar que el correo electrónico tiene formato válido y normalizarlo a minúsculas antes de procesar la inscripción.
- **FR-010**: El sistema DEBE validar que el evento referenciado existe; si no existe, responde con código 404.
- **FR-011**: El sistema DEBE responder con código 201 y los datos de la inscripción cuando la inscripción es exitosa.
- **FR-012**: El sistema DEBE responder con código 400 cuando faltan campos obligatorios (`eventId`, `buyerEmail`) o tienen formato inválido.
- **FR-013**: El sistema DEBE rechazar con código 422 la inscripción cuando la cantidad de inscripciones activas para un evento alcance el límite global configurado, indicando que la lista de espera está llena.

### Key Entities

- **WaitlistEntry**: Inscripción de un comprador en la lista de espera de un evento. Atributos clave: identificador único, referencia al evento, correo electrónico del comprador, estado (active, consumed, expired), fecha de creación.
- **Event** (existente): Evento al que pertenece la lista de espera. Atributos relevantes para esta feature: identificador, fecha del evento (usada para determinar si la lista está abierta o cerrada).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un comprador puede completar su inscripción en lista de espera en menos de 5 segundos desde el envío de la solicitud.
- **SC-002**: El 100% de intentos de inscripción duplicada son rechazados correctamente, incluso bajo concurrencia (dos solicitudes simultáneas con mismo correo y evento).
- **SC-003**: El 100% de intentos de inscripción en eventos con fecha pasada son rechazados sin excepción.
- **SC-004**: Un comprador con inscripción previamente consumida o expirada puede reinscribirse exitosamente en el primer intento.
- **SC-005**: Los mensajes de error devueltos por el sistema son suficientemente descriptivos para que el comprador entienda por qué su inscripción fue rechazada.

## Assumptions

- El catálogo de eventos ya existe en el sistema y es accesible para consulta (fecha del evento, existencia).
- No se requiere autenticación ni sesiones de usuario; el correo electrónico es el único identificador del comprador.
- La notificación al comprador cuando una entrada se libera (flujo posterior a la inscripción) está fuera del alcance de esta feature.
- La gestión de los estados "consumed" y "expired" de inscripciones existentes se realiza en otro flujo del sistema; esta feature solo lee esos estados para decidir si permitir reinscripción.
- La validación de formato de correo electrónico sigue el estándar RFC 5321 simplificado (presencia de "@", dominio válido), sin verificación de existencia del buzón.
- La determinación del cierre de la lista de espera se basa en la fecha y hora exacta del evento (datetime preciso) comparada con la fecha y hora actual del servidor.
- El límite máximo de inscripciones activas en la lista de espera es un valor global fijo aplicable a todos los eventos, definido como constante de configuración del sistema. El valor por defecto se determinará en la fase de planificación.
