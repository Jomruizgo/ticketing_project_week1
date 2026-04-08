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
- ¿Qué sucede si el flujo de reasignación falla técnicamente (error de red, BD no disponible)? El consumer rechaza (NACK) con `requeue: false` para evitar loops. El mensaje se pierde pero la oportunidad ya está expirada; la reasignación se puede reintentar manualmente o mediante un proceso de reconciliación.
- ¿Qué sucede si la publicación del evento de retorno al inventario falla? El fallo técnico se registra en logs. La oportunidad ya está expirada pero la entrada no vuelve al inventario automáticamente. El operador puede detectar el desajuste.
- ¿Qué sucede si el comprador cuya oportunidad expiró intenta reinscribirse? La inscripción fue marcada como `expired` al momento de la asignación en HU3. El comprador puede crear una nueva inscripción (HU1), siempre que la lista del evento siga vigente.

## Requirements

### Functional Requirements

- **FR-001**: El sistema DEBE consumir los mensajes de la cola `q.waitlist.opportunity.expired` (enrutados por DLX desde `q.waitlist.opportunity.delay`) e invocar el caso de uso de expiración.
- **FR-002**: El sistema DEBE transicionar la oportunidad de estado `active` a `expired` registrando el motivo (`ttl_expired`) y la marca de tiempo de expiración.
- **FR-003**: El sistema DEBE ser idempotente: si la oportunidad ya está en estado `expired` o `consumed`, confirmar (ACK) el mensaje sin modificación ni error.
- **FR-004**: Después de expirar una oportunidad, el sistema DEBE verificar si existe otro comprador con inscripción activa para el mismo evento.
- **FR-005**: Si hay comprador elegible, el sistema DEBE reutilizar el flujo de asignación de HU3 (mismos ticketId y eventId) para reasignar la entrada al siguiente comprador.
- **FR-006**: La reasignación DEBE seguir la política de priorización FIFO (orden de inscripción).
- **FR-007**: Si NO hay comprador elegible, el sistema DEBE publicar un evento de retorno al inventario para que la entrada vuelva a estar disponible para compra directa.
- **FR-008**: El evento de retorno al inventario DEBE contener: identificador de la entrada, identificador del evento y marca de tiempo del retorno.
- **FR-009**: Los fallos de validación de negocio (oportunidad ya expirada, ya consumida) DEBEN confirmarse (ACK). Los fallos técnicos DEBEN rechazarse (NACK) con `requeue: false`.
- **FR-010**: El período de validez de la oportunidad DEBE ser configurable mediante la variable de entorno `WAITLIST_OPPORTUNITY_TTL_MS` (por defecto 900000 ms = 15 minutos).

### Key Entities

- **WaitlistOpportunity**: Oportunidad de compra para un comprador de la lista de espera. Atributos relevantes: estado (`active`, `consumed`, `expired`), marca de tiempo de activación, marca de tiempo de expiración, motivo de expiración. Transición válida: `active → expired`.
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

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - [Brief Title] (Priority: P1)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently - e.g., "Can be fully tested by [specific action] and delivers [specific value]"]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]
2. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 2 - [Brief Title] (Priority: P2)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 3 - [Brief Title] (Priority: P3)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

[Add more user stories as needed, each with an assigned priority]

### Edge Cases

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right edge cases.
-->

- What happens when [boundary condition]?
- How does system handle [error scenario]?

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

- **FR-001**: System MUST [specific capability, e.g., "allow users to create accounts"]
- **FR-002**: System MUST [specific capability, e.g., "validate email addresses"]  
- **FR-003**: Users MUST be able to [key interaction, e.g., "reset their password"]
- **FR-004**: System MUST [data requirement, e.g., "persist user preferences"]
- **FR-005**: System MUST [behavior, e.g., "log all security events"]

*Example of marking unclear requirements:*

- **FR-006**: System MUST authenticate users via [NEEDS CLARIFICATION: auth method not specified - email/password, SSO, OAuth?]
- **FR-007**: System MUST retain user data for [NEEDS CLARIFICATION: retention period not specified]

### Key Entities *(include if feature involves data)*

- **[Entity 1]**: [What it represents, key attributes without implementation]
- **[Entity 2]**: [What it represents, relationships to other entities]

## Success Criteria *(mandatory)*

<!--
  ACTION REQUIRED: Define measurable success criteria.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: [Measurable metric, e.g., "Users can complete account creation in under 2 minutes"]
- **SC-002**: [Measurable metric, e.g., "System handles 1000 concurrent users without degradation"]
- **SC-003**: [User satisfaction metric, e.g., "90% of users successfully complete primary task on first attempt"]
- **SC-004**: [Business metric, e.g., "Reduce support tickets related to [X] by 50%"]

## Assumptions

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right assumptions based on reasonable defaults
  chosen when the feature description did not specify certain details.
-->

- [Assumption about target users, e.g., "Users have stable internet connectivity"]
- [Assumption about scope boundaries, e.g., "Mobile support is out of scope for v1"]
- [Assumption about data/environment, e.g., "Existing authentication system will be reused"]
- [Dependency on existing system/service, e.g., "Requires access to the existing user profile API"]
