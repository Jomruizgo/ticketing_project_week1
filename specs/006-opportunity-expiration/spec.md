# Feature Specification: Expiración de Oportunidad de Lista de Espera

**Feature Branch**: `006-opportunity-expiration`  
**Created**: 2026-04-08  
**Status**: Draft  
**Input**: User description: "Construye una feature de expiración de oportunidades para el sistema de lista de espera. Cuando el período de validez de una oportunidad activa (por defecto 15 minutos, configurable mediante WAITLIST_OPPORTUNITY_TTL_MS) expira sin que el comprador la reclame, el sistema la marca automáticamente como expirada. La expiración se detecta mediante un mecanismo Dead Letter Exchange (DLX): cuando la oportunidad fue creada, se publicó un mensaje a una cola delay con un TTL que coincide con el período de validez; cuando el TTL expira, RabbitMQ enruta el mensaje a una cola de expiración. Al procesar la expiración: el estado de la oportunidad cambia a 'expired' con el motivo y la marca de tiempo exacta registrados. Luego el sistema verifica si otro comprador tiene una inscripción activa para el mismo evento. Si la hay, reasigna la entrada al siguiente comprador elegible reutilizando el mismo flujo de asignación de HU3 (prioridad FIFO, reserva temporal, creación de oportunidad). Si no hay comprador elegible, la entrada vuelve al inventario general disponible para compra directa. La inscripción del comprador cuya oportunidad expiró permanece inactiva, pero puede reinscribirse. El ciclo de expiración continúa hasta que un comprador reclame una oportunidad o no haya más inscripciones y la entrada vuelva al inventario abierto."

## User Scenarios & Testing

### User Story 1 — Expiración automática de oportunidad no reclamada (Priority: P1)

Como sistema, cuando el período de validez de una oportunidad activa se cumple sin que el comprador la reclame, marco la oportunidad como expirada con el motivo y la marca de tiempo exacta. La inscripción del comprador cuya oportunidad expiró permanece inactiva (fue inactivada al asignarse en HU3), habilitando una futura reinscripción mientras la lista del evento siga vigente.

**Why this priority**: Sin expiración, una oportunidad no utilizada bloquea indefinidamente una entrada, paralizando la reasignación y el retorno al inventario.

**Independent Test**: Crear una oportunidad activa, esperar a que el TTL venza (o simular el mensaje DLX), y verificar que la oportunidad cambia a estado `expired` con motivo `ttl_expired` y marca de tiempo registrada.

**Acceptance Scenarios**:

1. **Given** una oportunidad en estado `active` cuyo período de validez ha expirado, **When** el mensaje DLX llega a la cola de expiración y el consumer lo procesa, **Then** la oportunidad transiciona a estado `expired` con motivo y marca de tiempo registrados.
2. **Given** una oportunidad que ya fue expirada previamente, **When** el sistema recibe un segundo mensaje de expiración para la misma oportunidad, **Then** el sistema reconoce la idempotencia, confirma (ACK) el mensaje sin error y no modifica el estado.
3. **Given** una oportunidad en estado `consumed` (el comprador ya la reclamó), **When** el mensaje DLX llega tarde, **Then** el sistema confirma (ACK) el mensaje sin modificar el estado de la oportunidad.

---

### User Story 2 — Reasignación al siguiente comprador elegible (Priority: P2)

Como sistema, después de expirar una oportunidad, verifico si hay otro comprador con inscripción activa para el mismo evento. Si lo hay, reasigno la entrada al siguiente comprador elegible reutilizando el flujo de asignación existente (prioridad FIFO, reserva temporal, creación de nueva oportunidad).

**Why this priority**: La reasignación maximiza la probabilidad de venta al dar la oportunidad al siguiente comprador en la cola, sin intervención manual.

**Independent Test**: Crear una oportunidad activa para un evento donde existe otro comprador con inscripción activa. Expirar la oportunidad y verificar que el sistema invoca el flujo de asignación y genera una nueva oportunidad para el siguiente comprador elegible.

**Acceptance Scenarios**:

1. **Given** una oportunidad que acaba de expirar y otro comprador con inscripción activa en la lista de espera del mismo evento, **When** el sistema procesa la expiración, **Then** invoca el flujo de asignación existente con el mismo ticketId y eventId, y una nueva oportunidad es creada para el siguiente comprador elegible.
2. **Given** una oportunidad que acaba de expirar y múltiples compradores con inscripción activa, **When** el sistema reasigna, **Then** la selección sigue la política FIFO (el comprador que se inscribió primero recibe la oportunidad).

---

### User Story 3 — Devolución al inventario cuando no hay compradores elegibles (Priority: P3)

Como sistema, después de expirar una oportunidad, si no hay ningún comprador con inscripción activa para el mismo evento, devuelvo la entrada al inventario general para que quede disponible para compra directa.

**Why this priority**: Sin esta lógica, una entrada quedaría en un estado limbo cuando la lista de espera se vacía.

**Independent Test**: Crear una oportunidad activa para un evento sin más inscripciones activas. Expirar la oportunidad y verificar que se publica el evento de retorno al inventario y la entrada queda disponible para compra directa.

**Acceptance Scenarios**:

1. **Given** una oportunidad que acaba de expirar y ningún comprador con inscripción activa en la lista de espera del mismo evento, **When** el sistema procesa la expiración, **Then** publica un evento de retorno al inventario para que la entrada vuelva a estar disponible para compra directa.
2. **Given** una oportunidad expirada cuya entrada fue devuelta al inventario, **Then** la entrada queda libre de cualquier reserva o asociación con la lista de espera.

---

### Edge Cases

- ¿Qué sucede si el mensaje DLX llega pero la oportunidad ya fue consumida (el comprador reclamó justo antes del vencimiento)? El sistema verifica el estado actual: si es `consumed`, confirma (ACK) sin modificar. La idempotencia protege contra condiciones de carrera.
- ¿Qué sucede si el flujo de reasignación falla técnicamente (error de red, BD no disponible)? El consumer rechaza (NACK) con `requeue: false`. Se registra log estructurado con nivel `Error` y se incrementa el contador `expiration_reassignment_failures_total`. La oportunidad ya está expirada; la reconciliación queda pendiente como deuda técnica.
- ¿Qué sucede si la publicación del evento de retorno al inventario falla? Se registra log estructurado con nivel `Error` y se incrementa el contador `expiration_inventory_return_failures_total`. La oportunidad ya está expirada pero la entrada no vuelve al inventario automáticamente. La reconciliación queda pendiente como deuda técnica.
- ¿Qué sucede si el comprador cuya oportunidad expiró intenta reinscribirse? La inscripción fue marcada como `expired` al momento de la asignación en HU3. El comprador puede crear una nueva inscripción (HU1), siempre que la lista del evento siga vigente.

## Clarifications

### Session 2026-04-08

- Q: FR-002 exige registrar motivo y timestamp de expiración, pero R3 de research.md decidió NO agregar columnas. ¿Dónde se persiste? → A: Agregar columnas `expired_at` (nullable DateTime) y `expiration_reason` (nullable string) a `WaitlistOpportunity`. FR-002 prevalece; la trazabilidad en BD es necesaria para auditoría y diagnóstico.
- Q: ¿Cómo distingue el handler los resultados de `IAssignOpportunityUseCase` para decidir si publicar retorno a inventario o NACK? → A: Usar resultado tipado existente `AssignOpportunityResult` con `AssignOpportunityResultType` (`Assigned` → no-op, `NoEligible`/`AllFailed` → publicar `ticket.returned_to_inventory`, excepción → propagar para NACK).
- Q: ¿Es suficiente solo logging ante fallos técnicos de reasignación/retorno a inventario, o se necesita un mecanismo de reconciliación? → A: Logging estructurado (Error level) + contadores de métricas para monitoreo en esta HU. Registrar un job de reconciliación periódico como deuda técnica explícita para una HU futura.

## Requirements

### Functional Requirements

- **FR-001**: El sistema DEBE consumir los mensajes de la cola `q.waitlist.opportunity.expired` (enrutados por DLX desde `q.waitlist.opportunity.delay`) e invocar el caso de uso de expiración.
- **FR-002**: El sistema DEBE transicionar la oportunidad de estado `active` a `expired` registrando el motivo (`ttl_expired`) y la marca de tiempo de expiración.
- **FR-003**: El sistema DEBE ser idempotente: si la oportunidad ya está en estado `expired` o `consumed`, confirmar (ACK) el mensaje sin modificación ni error.
- **FR-004**: Después de expirar una oportunidad, el sistema DEBE verificar si existe otro comprador con inscripción activa para el mismo evento.
- **FR-005**: Si hay comprador elegible, el sistema DEBE reutilizar el flujo de asignación de HU3 (mismos ticketId y eventId) y evaluar el `AssignOpportunityResult`: `Assigned` → reasignación exitosa sin acción adicional; `NoEligible` o `AllFailed` → publicar retorno al inventario; excepción técnica → propagar para NACK del mensaje.
- **FR-006**: La reasignación DEBE seguir la política de priorización FIFO (orden de inscripción).
- **FR-007**: Si NO hay comprador elegible, el sistema DEBE publicar un evento de retorno al inventario para que la entrada vuelva a estar disponible para compra directa.
- **FR-008**: El evento de retorno al inventario DEBE contener: identificador del ticket (`ticketId`), identificador del evento (`eventId`) y marca de tiempo del retorno (`returnedAt`).
- **FR-009**: Los fallos de validación de negocio (oportunidad ya expirada, ya consumida) DEBEN confirmarse (ACK). Los fallos técnicos DEBEN rechazarse (NACK) con `requeue: false`.
- **FR-010**: El período de validez de la oportunidad DEBE ser configurable mediante la variable de entorno `WAITLIST_OPPORTUNITY_TTL_MS` (por defecto 900000 ms = 15 minutos).
- **FR-011**: El handler DEBE registrar logs estructurados con nivel `Error` ante fallos técnicos de reasignación o publicación de retorno a inventario, y DEBE incrementar contadores de métricas (`expiration_reassignment_failures_total`, `expiration_inventory_return_failures_total`) para monitoreo operacional. **Deuda técnica**: un job de reconciliación periódico que detecte oportunidades expiradas cuyo ticket no fue reasignado ni devuelto a inventario se registra como trabajo futuro.

### Key Entities

- **WaitlistOpportunity**: Oportunidad de compra para un comprador de la lista de espera. Atributos relevantes: estado (`active`, `consumed`, `expired`), marca de tiempo de activación (`activated_at`), marca de tiempo de vencimiento (`expires_at`), marca de tiempo de expiración efectiva (`expired_at`, nullable), motivo de expiración (`expiration_reason`, nullable string, e.g. `ttl_expired`). Transición válida: `active → expired`. Las columnas `expired_at` y `expiration_reason` se agregan como parte de esta HU.
- **WaitlistEntry**: Inscripción de un comprador en la lista de espera de un evento. El estado pasa de `active` a `expired` cuando se asigna una oportunidad (HU3). El comprador puede reinscribirse creando una nueva entrada.

## Success Criteria

### Measurable Outcomes

- **SC-001**: El 100% de las oportunidades cuyo TTL vence sin reclamación transicionan automáticamente a estado `expired` en menos de 5 segundos desde la llegada del mensaje DLX.
- **SC-002**: El 100% de las expiraciones con comprador elegible activan una reasignación exitosa utilizando el flujo existente.
- **SC-003**: El 100% de las expiraciones sin comprador elegible resultan en la publicación del evento de retorno al inventario.
- **SC-004**: El procesamiento de mensajes duplicados de expiración no genera errores ni estados inconsistentes (idempotencia verificable al 100%).
- **SC-005**: El ciclo expiración → reasignación continúa hasta que un comprador reclame la oportunidad o no haya más inscripciones activas.

## Assumptions

- La topología DLX ya está declarada en `scripts/setup-rabbitmq.sh`: `q.waitlist.opportunity.delay` (con `x-message-ttl` y DLX) → `q.waitlist.opportunity.expired`.
- El mensaje de delay se publica cuando la oportunidad transiciona a `active` (HU3), no cuando se crea como `pending`.
- El flujo de asignación de HU3 está disponible como caso de uso reutilizable con la misma interfaz de entrada (ticketId, eventId).
- La inscripción del comprador cuya oportunidad expiró ya fue marcada como `expired` en HU3 al momento de la asignación. No se requiere cambio de estado adicional en la inscripción durante la expiración de la oportunidad.
- El evento de retorno al inventario usa la routing key `ticket.returned_to_inventory` en el exchange `tickets`, consistente con la topología documentada en Planning2.md.
- El consumer que procesa `ticket.returned_to_inventory` ya existe o se creará como parte de la infraestructura compartida (fuera del alcance funcional de esta HU, pero necesario para el efecto end-to-end).
