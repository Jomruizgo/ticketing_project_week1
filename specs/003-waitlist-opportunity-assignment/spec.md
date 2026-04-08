# Feature Specification: Asignación de Oportunidad de Lista de Espera

**Feature Branch**: `003-waitlist-opportunity-assignment`  
**Created**: 2026-04-07  
**Status**: Draft  
**Input**: User description: "Construye una feature de asignación de oportunidades para el sistema de lista de espera. Cuando una entrada queda libre (reserva expirada sin pago o pago rechazado), el sistema la detecta automáticamente mediante un consumer de RabbitMQ que escucha la routing key ticket.released. Luego verifica si algún comprador tiene una inscripción activa en la lista de espera para ese evento. Si existen compradores elegibles, el sistema selecciona al siguiente usando una estrategia de prioridad FIFO basada en el momento de inscripción. Reserva temporalmente la entrada para ese comprador y crea una oportunidad en lista de espera con estado 'active' y un período de validez configurable (por defecto 15 minutos mediante WAITLIST_OPPORTUNITY_TTL_MS). La inscripción del comprador pasa a 'consumed' al activarse la oportunidad. Si la reserva temporal falla, no se crea ninguna oportunidad y la inscripción permanece activa para el siguiente intento; el fallo queda registrado para diagnóstico. Si no hay comprador elegible, la entrada vuelve al inventario general disponible para compra directa. Dos liberaciones simultáneas para el mismo evento no pueden asignarse al mismo comprador: garantizado por una restricción única que permite solo una oportunidad activa por inscripción. El sistema publica un evento opportunity_activated para consumidores downstream (notificaciones)."

## Clarifications

### Session 2026-04-07

- Q: ¿Cuando la reserva temporal falla, el sistema intenta con el siguiente elegible inmediatamente o espera un nuevo evento? → A: Iterar al siguiente comprador elegible en el mismo ciclo; si todos fallan, devolver al inventario.
- Q: ¿La oportunidad se crea directamente como "active" o existe una transición pending→active? → A: Se crea como "pending" (estado transitorio mientras se confirma la reserva); transiciona a "active" tras confirmar la reserva exitosamente, o a "failed" si la reserva falla. Definido en Planning2.md (glosario: "Oportunidad en proceso", patrón State, topología DLX).
- Q: ¿Cuál es el payload mínimo del evento `opportunity_activated` (RabbitMQ)? → A: `{ opportunityId, waitlistEntryId, ticketId, eventId, buyerEmail, activatedAt, expiresAt }`. Superset del SSE definido en API_CONTRACTS.md, agrega `buyerEmail` (necesario para HU5/correo), `waitlistEntryId` (trazabilidad) y `activatedAt` (marca temporal cruda).
- Q: ¿El consumer de `ticket.released` procesa mensajes secuencialmente o concurrentemente? → A: `prefetchCount = 1` (secuencial). El volumen de liberaciones es bajo; el procesamiento secuencial simplifica la lógica de iteración FIFO y evita conflictos de unicidad entre hilos del mismo consumer. La restricción única de BD queda como segunda línea de defensa.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Asignación exitosa al siguiente comprador elegible (Priority: P1)

Una entrada queda libre porque una reserva expiró sin pago o un pago fue rechazado. El sistema detecta la liberación, busca al comprador con la inscripción activa más antigua en la lista de espera de ese evento, reserva temporalmente la entrada para ese comprador, y crea una oportunidad con estado "active" y una vigencia configurada. La inscripción del comprador pasa a "consumed" para habilitar futura reinscripción. El sistema publica un evento para que consumidores downstream (como notificaciones) reaccionen.

**Why this priority**: Sin esta historia, la lista de espera no puede convertir demanda pendiente en oportunidades de compra. Es la razón de ser de la feature y el único camino que transforma una inscripción pasiva en una acción concreta del comprador.

**Independent Test**: Se puede verificar publicando un mensaje `ticket.released` en RabbitMQ para un evento que tiene al menos una inscripción activa, y comprobando que se crea una oportunidad "active" vinculada a la inscripción más antigua, que la entrada queda reservada temporalmente, que la inscripción pasa a "consumed", y que se publica el evento `opportunity_activated`.

**Acceptance Scenarios**:

1. **Given** una entrada liberada para el evento 42 y un comprador con inscripción activa registrada hace 10 minutos (la más antigua), **When** el sistema procesa el mensaje `ticket.released`, **Then** se crea una oportunidad con estado "active", vigencia de 15 minutos desde la activación, vinculada a esa inscripción y a esa entrada; la inscripción pasa a estado "consumed"; y se publica el evento `opportunity_activated` con los datos de la oportunidad.
2. **Given** tres compradores con inscripciones activas para el evento 42 registradas en momentos distintos, **When** el sistema procesa un mensaje `ticket.released`, **Then** la oportunidad se asigna al comprador cuya inscripción fue creada primero (FIFO por momento de inscripción).
3. **Given** una entrada liberada para el evento 42 y un comprador elegible, **When** el sistema crea la oportunidad exitosamente, **Then** la entrada queda en estado "reserved" exclusivamente para ese comprador y no puede ser asignada a otro comprador ni adquirida por compra directa durante la vigencia de la oportunidad.

---

### User Story 2 — Devolución al inventario cuando no hay compradores elegibles (Priority: P2)

Una entrada queda libre pero ningún comprador tiene inscripción activa en la lista de espera para ese evento. El sistema devuelve la entrada al inventario general para que quede disponible para compra directa.

**Why this priority**: Complementa la historia principal garantizando que las entradas liberadas no queden bloqueadas indefinidamente cuando la lista de espera está vacía. Sin esta historia, las entradas liberadas sin demanda pendiente desaparecen del flujo de venta.

**Independent Test**: Se puede verificar publicando un mensaje `ticket.released` para un evento sin inscripciones activas en la lista de espera, y comprobando que la entrada vuelve a estar disponible para compra directa en el inventario general.

**Acceptance Scenarios**:

1. **Given** una entrada liberada para el evento 42 y ninguna inscripción activa en la lista de espera de ese evento, **When** el sistema procesa el mensaje `ticket.released`, **Then** el sistema publica un evento `ticket.returned_to_inventory` para que la entrada vuelva al inventario general disponible para compra directa, y no se crea ninguna oportunidad.
2. **Given** una entrada liberada para el evento 42 donde todas las inscripciones existentes tienen estado "consumed" o "expired", **When** el sistema procesa el mensaje `ticket.released`, **Then** el sistema trata el escenario como si no hubiera compradores elegibles y devuelve la entrada al inventario general.

---

### User Story 3 — Manejo de fallo en la reserva temporal (Priority: P2)

El sistema identifica un comprador elegible para una entrada liberada, pero el intento de reservar temporalmente la entrada falla (por ejemplo, conflicto de concurrencia, la entrada ya fue tomada por otro proceso). En este caso, la oportunidad creada como "pending" transiciona a "failed" para trazabilidad (ver FR-003); la inscripción del comprador permanece activa para ser considerada en el próximo intento, y el fallo queda registrado para diagnóstico.

**Why this priority**: Protege la integridad del sistema ante fallos técnicos. Sin esta historia, un fallo en la reserva podría generar oportunidades sin respaldo real en el inventario o dejar inscripciones en un estado inconsistente.

**Independent Test**: Se puede verificar forzando un fallo en el mecanismo de reserva temporal (simulando conflicto de concurrencia) y comprobando que no se crea oportunidad, la inscripción sigue activa, y se registra un log de diagnóstico.

**Acceptance Scenarios**:

1. **Given** una entrada liberada para el evento 42 y un comprador elegible con inscripción activa, **When** el sistema intenta reservar la entrada y la operación falla por conflicto de concurrencia, **Then** la oportunidad creada como "pending" transiciona a "failed" para trazabilidad, la inscripción permanece en estado "active", el fallo queda registrado con suficiente detalle para diagnóstico, y el sistema intenta inmediatamente con el siguiente comprador elegible en la fila.
2. **Given** un fallo en la reserva temporal para todos los compradores elegibles en el mismo ciclo, **When** no queda ningún elegible por intentar, **Then** la entrada se devuelve al inventario general mediante `ticket.returned_to_inventory`.
3. **Given** un fallo en la reserva temporal para el comprador elegible, **When** una nueva entrada se libera en el mismo evento posteriormente, **Then** el mismo comprador vuelve a ser considerado como elegible (su inscripción sigue activa).

---

### User Story 4 — Protección contra asignación duplicada simultánea (Priority: P3)

Dos entradas se liberan al mismo tiempo para el mismo evento. El sistema garantiza que un mismo comprador no reciba dos oportunidades activas simultáneamente. La restricción se aplica a nivel de datos: solo puede existir una oportunidad activa por inscripción.

**Why this priority**: Escenario de borde que previene inconsistencias en condiciones de concurrencia. Aunque menos frecuente que los flujos principales, la ausencia de esta protección podría generar reservas dobles que afecten la confianza del comprador y la integridad del inventario.

**Independent Test**: Se puede verificar simulando dos mensajes `ticket.released` procesados concurrentemente para el mismo evento con un único comprador elegible, y comprobando que solo se crea una oportunidad activa.

**Acceptance Scenarios**:

1. **Given** dos entradas liberadas simultáneamente para el evento 42 y un solo comprador con inscripción activa, **When** el sistema procesa ambos mensajes, **Then** solo se crea una oportunidad activa para ese comprador; la segunda entrada se devuelve al inventario general o se asigna al siguiente comprador elegible si existe.
2. **Given** dos entradas liberadas simultáneamente para el evento 42 y dos compradores con inscripciones activas, **When** el sistema procesa ambos mensajes, **Then** cada comprador recibe como máximo una oportunidad activa, asignadas en orden FIFO.

---

### Edge Cases

- ¿Qué sucede si el evento asociado a la entrada liberada no existe en el sistema? El sistema ignora el mensaje, registra un log de advertencia y hace ACK para no reprocesarlo.
- ¿Qué sucede si el ticket referenciado en el mensaje `ticket.released` no existe? El sistema ignora el mensaje, registra un log de advertencia y hace ACK.
- ¿Qué sucede si la lista de espera del evento ya cerró (la fecha del evento fue alcanzada) cuando llega un `ticket.released`? El sistema no busca elegibles, devuelve la entrada al inventario general y registra el motivo.
- ¿Qué sucede si el mismo mensaje `ticket.released` llega duplicado (redelivery por RabbitMQ)? El sistema maneja la idempotencia: si ya existe una oportunidad activa para ese ticket, ignora el mensaje duplicado.
- ¿Qué sucede si la inscripción del comprador elegible fue modificada por otro proceso entre la selección y la creación de la oportunidad? El bloqueo optimista o la restricción única previenen inconsistencias; el sistema reintenta con el siguiente elegible o devuelve al inventario.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema DEBE consumir mensajes con routing key `ticket.released` desde el exchange `tickets` de RabbitMQ para detectar entradas liberadas automáticamente.
- **FR-002**: Al detectar una entrada liberada, el sistema DEBE buscar la inscripción activa más antigua en la lista de espera del evento asociado, usando orden de llegada (FIFO por `enrolled_at`).
- **FR-003**: Si existe un comprador elegible, el sistema DEBE crear la oportunidad en estado "pending" y luego intentar reservar temporalmente la entrada para ese comprador. Si la reserva se confirma, la oportunidad transiciona a "active". Si la reserva falla, la oportunidad transiciona a "failed" y la inscripción permanece activa.
- **FR-004**: El sistema DEBE crear una oportunidad vinculada a la inscripción y a la entrada. La vigencia configurable mediante `WAITLIST_OPPORTUNITY_TTL_MS` (por defecto 900000 ms = 15 minutos) comienza a contar desde la transición a "active", no desde la creación como "pending".
- **FR-005**: Al crear la oportunidad exitosamente, la inscripción del comprador DEBE pasar a estado "consumed" para reflejar que fue atendida y habilitar reinscripción futura.
- **FR-006**: El sistema DEBE publicar un evento `opportunity_activated` (routing key: `waitlist.opportunity.activated`) tras activar una oportunidad, con payload: `{ opportunityId, waitlistEntryId, ticketId, eventId, buyerEmail, activatedAt, expiresAt }`. Este payload permite que consumidores downstream (notificaciones in-app y correo) reaccionen con toda la información necesaria.
- **FR-007**: El mensaje publicado en el delay queue para expiración DEBE enviarse cuando la oportunidad transiciona a "active", para que el TTL cuente desde la activación real.
- **FR-008**: Si no existen compradores con inscripción activa, el sistema DEBE publicar un evento `ticket.returned_to_inventory` para devolver la entrada al inventario general.
- **FR-009**: Si la reserva temporal falla, la oportunidad creada como "pending" (FR-003) DEBE transicionar a "failed" para trazabilidad; la inscripción DEBE permanecer en estado "active". El sistema DEBE intentar inmediatamente con el siguiente comprador elegible en la fila (mismo ciclo de procesamiento). Si no quedan elegibles, la entrada se devuelve al inventario general.
- **FR-010**: El sistema DEBE registrar los fallos de reserva temporal con detalle suficiente para diagnóstico (identificadores de evento, ticket y comprador, motivo del fallo).
- **FR-011**: El sistema DEBE garantizar que solo puede existir una oportunidad activa por inscripción, usando una restricción única a nivel de datos para prevenir asignaciones duplicadas bajo concurrencia.
- **FR-012**: El sistema DEBE ser idempotente ante mensajes `ticket.released` duplicados: si ya existe una oportunidad activa para el mismo ticket, el mensaje duplicado se ignora y se hace ACK.
- **FR-013**: El sistema DEBE ignorar mensajes `ticket.released` para eventos cuya fecha ya fue alcanzada (lista de espera cerrada), devolviendo la entrada al inventario general.
- **FR-014**: Los fallos de validación de negocio (evento inexistente, ticket inexistente, lista cerrada, sin elegibles) DEBEN resultar en ACK del mensaje; los fallos técnicos inesperados DEBEN resultar en NACK sin requeue.
- **FR-015**: La selección del comprador elegible DEBE ser extensible mediante una estrategia de priorización. La implementación inicial es FIFO por momento de inscripción.
- **FR-016**: El consumer de `ticket.released` DEBE configurarse con `prefetchCount = 1` para garantizar procesamiento secuencial de mensajes. La restricción única de BD actúa como segunda línea de defensa ante concurrencia externa (múltiples instancias del servicio).

### Key Entities

- **WaitlistOpportunity**: Representa una oportunidad de compra asignada a un comprador desde la lista de espera. Atributos clave: identificador, referencia a la inscripción, referencia a la entrada, estado (pending, active, consumed, expired, failed), momento de activación, momento de expiración. El estado "pending" es transitorio durante la confirmación de reserva; "active" se alcanza tras reserva exitosa; "failed" si la reserva falla.
- **WaitlistEntry**: Inscripción existente del comprador en la lista de espera. Esta feature consume y modifica su estado (de "active" a "consumed" al asignar oportunidad).
- **Ticket**: Entrada liberada que dispara el proceso. Esta feature la reserva temporalmente para el comprador elegible.
- **TicketReleasedEvent**: Mensaje recibido desde RabbitMQ que contiene el identificador del ticket, el identificador del evento y el momento de la liberación.
- **OpportunityActivatedEvent**: Mensaje publicado por esta feature para notificar a consumidores downstream que una oportunidad fue activada. Payload: `{ opportunityId, waitlistEntryId, ticketId, eventId, buyerEmail, activatedAt, expiresAt }`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Cuando una entrada se libera y existe un comprador elegible, la oportunidad se crea y la entrada queda reservada en menos de 5 segundos desde la recepción del mensaje.
- **SC-002**: El 100% de las entradas liberadas sin compradores elegibles vuelven al inventario general sin intervención manual.
- **SC-003**: Bajo condiciones de concurrencia (dos liberaciones simultáneas para el mismo evento), el sistema nunca asigna dos oportunidades activas al mismo comprador.
- **SC-004**: Cada fallo en la reserva temporal queda registrado con información suficiente para que el equipo de soporte pueda diagnosticar la causa sin revisar logs de otros servicios.
- **SC-005**: Los mensajes `ticket.released` duplicados no generan oportunidades duplicadas ni efectos secundarios observables.
- **SC-006**: La vigencia de la oportunidad es configurable sin necesidad de modificar código ni realizar un despliegue.

## Assumptions

- La topología de RabbitMQ (exchange `tickets`, cola `q.ticket.released` con binding `ticket.released`, cola `q.waitlist.opportunity.delay` con TTL y DLX, cola `q.ticket.returned` con binding `ticket.returned_to_inventory`) se declara en `scripts/setup-rabbitmq.sh` como prerequisito antes de que esta feature se despliegue.
- ReservationService y paymentService ya publican el evento `ticket.released` cuando una reserva expira o un pago es rechazado. Esta feature solo consume ese evento, no lo produce.
- La reserva temporal de la entrada opera sobre tickets en estado `released` (`WHERE status = 'released'`), mientras que la reserva directa sigue operando sobre `available`. El flujo de compra existente no se modifica.
- La tabla `waitlist_opportunities` ya existe en el esquema de la base de datos (creada como parte de la feature 002).
- La inscripción del comprador cambia a estado "consumed" (no "inactive") al asignar la oportunidad, siguiendo el enum `WaitlistEntryStatus` existente que define los valores `active`, `consumed`, `expired`.
- El payload del evento `ticket.released` contiene como mínimo: `ticketId` (int), `eventId` (int), `releasedAt` (datetime).
- La política ACK/NACK sigue la convención del proyecto: fallos de validación de negocio → ACK; fallos técnicos inesperados → NACK sin requeue.
- El sistema opera contra una base de datos PostgreSQL compartida; las restricciones de unicidad y bloqueo optimista se implementan a nivel de base de datos.
- El correo electrónico del comprador ya está normalizado a minúsculas en la inscripción (resuelto en la feature 001).
