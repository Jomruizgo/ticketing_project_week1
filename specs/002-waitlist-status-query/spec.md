# Feature Specification: Consulta de Estado de Lista de Espera

**Feature Branch**: `002-waitlist-status-query`  
**Created**: 2026-04-07  
**Status**: Draft  
**Input**: User description: "Construye una feature de consulta de estado para el sistema de lista de espera. Un comprador inscrito puede verificar su estado actual proporcionando su correo electrónico y eventId. El sistema devuelve el estado de inscripción del comprador y, si existe una oportunidad, su estado actual (activa con tiempo restante, consumida o expirada). Cuando la oportunidad está activa, la respuesta incluye los minutos restantes de validez e indica que una entrada está temporalmente reservada. Cuando expira, no se muestra ninguna reserva. Cuando no existe inscripción, el sistema devuelve 404. Los cuatro estados posibles que puede ver el comprador son: inscripción activa (en espera), oportunidad activa (entrada reservada, tiempo en curso), oportunidad consumida (avanzó al pago), oportunidad expirada (el tiempo se agotó). El endpoint de la API es GET /api/waitlist/entries?eventId={id}&email={email}."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Consulta de inscripción activa sin oportunidad (Priority: P1)

Un comprador que se inscribió previamente en la lista de espera de un evento quiere verificar su estado actual. Proporciona su correo electrónico y el identificador del evento. El sistema localiza su inscripción y le informa que está activo en la lista de espera, sin oportunidad de compra asignada aún.

**Why this priority**: Es el caso base y más frecuente. La mayoría de compradores inscritos estarán en estado de espera sin oportunidad asignada. Sin esta historia, la feature no entrega ningún valor.

**Independent Test**: Se puede probar creando una inscripción activa (vía POST /api/waitlist/entries) y luego consultando con GET. Se verifica que la respuesta contiene los datos de inscripción y que no hay oportunidad asociada.

**Acceptance Scenarios**:

1. **Given** un comprador con inscripción activa en el evento 42 y sin oportunidad asignada, **When** consulta `GET /api/waitlist/entries?eventId=42&email=comprador@ejemplo.com`, **Then** el sistema responde con código 200, los datos de inscripción (id, eventId, buyerEmail, status "active", enrolledAt) y oportunidad nula.
2. **Given** un comprador con inscripción activa, **When** consulta con los mismos parámetros múltiples veces, **Then** cada respuesta es idéntica y consistente (operación idempotente de lectura).

---

### User Story 2 — Consulta con oportunidad activa (Priority: P1)

Un comprador inscrito recibe una oportunidad de compra. Al consultar su estado, el sistema le muestra que tiene una entrada temporalmente reservada con un tiempo de validez en curso. La respuesta incluye los minutos restantes antes de que la oportunidad expire.

**Why this priority**: Igual de crítica que la anterior — es la información que permite al comprador actuar (reclamar la oportunidad). Sin esta información, el comprador no sabe que tiene una ventana de acción limitada.

**Independent Test**: Se puede probar con una inscripción que tenga una oportunidad activa asociada. Se verifica que la respuesta incluye datos de la oportunidad, el tiempo restante calculado dinámicamente, y el estado "active".

**Acceptance Scenarios**:

1. **Given** un comprador con inscripción activa y una oportunidad en estado "active" que expira en 12 minutos, **When** consulta su estado, **Then** el sistema responde con código 200, los datos de inscripción, y los datos de oportunidad incluyendo estado "active", fecha de activación, fecha de expiración y minutos restantes (12).
2. **Given** una oportunidad activa con menos de 1 minuto restante, **When** el comprador consulta su estado, **Then** el sistema muestra los minutos restantes redondeados a 0 o 1, reflejando que la ventana está a punto de cerrarse.

---

### User Story 3 — Consulta cuando no existe inscripción (Priority: P2)

Un comprador que no se ha inscrito en la lista de espera (o proporciona datos incorrectos) intenta consultar su estado. El sistema responde indicando que no se encontró ninguna inscripción.

**Why this priority**: Es el caso de error más común y necesario para una UX clara. El comprador necesita saber que no está inscrito para poder inscribirse.

**Independent Test**: Se puede probar consultando con un email/eventId que no tiene inscripción asociada. Se verifica que la respuesta es 404.

**Acceptance Scenarios**:

1. **Given** que no existe inscripción para el email "nadie@ejemplo.com" en el evento 42, **When** consulta `GET /api/waitlist/entries?eventId=42&email=nadie@ejemplo.com`, **Then** el sistema responde con código 404 y un mensaje indicando que no se encontró inscripción.
2. **Given** que existe una inscripción para el evento 42 pero no para el evento 99, **When** el comprador consulta con eventId=99, **Then** el sistema responde con 404.

---

### User Story 4 — Consulta con oportunidad consumida o expirada (Priority: P2)

Un comprador que ya tuvo una oportunidad asignada consulta su estado. Si la oportunidad fue consumida (avanzó al pago), el sistema muestra el estado "consumed". Si la oportunidad expiró sin ser reclamada, el sistema muestra el estado "expired" y no muestra ninguna reserva asociada.

**Why this priority**: Cubre los estados terminales de la oportunidad. Importante para que el comprador entienda qué pasó con su oportunidad, pero no habilita acción adicional dentro de esta feature.

**Independent Test**: Se puede probar con inscripciones que tengan oportunidades en estado "consumed" y "expired" respectivamente.

**Acceptance Scenarios**:

1. **Given** un comprador cuya oportunidad fue consumida (avanzó al pago), **When** consulta su estado, **Then** el sistema responde con código 200, los datos de inscripción, y la oportunidad con estado "consumed".
2. **Given** un comprador cuya oportunidad expiró, **When** consulta su estado, **Then** el sistema responde con código 200, los datos de inscripción, y la oportunidad con estado "expired" sin indicación de reserva ni tiempo restante.

---

### Edge Cases

- ¿Qué ocurre si se proporcionan parámetros `eventId` o `email` vacíos o ausentes? El sistema debe responder con 400 (Bad Request) indicando los campos obligatorios faltantes.
- ¿Qué ocurre si el `eventId` no corresponde a un evento existente? El sistema responde 404 indicando que no se encontró inscripción (no se expone la inexistencia del evento para evitar enumeración).
- ¿Qué ocurre si el formato del email es inválido? El sistema responde con 400 indicando formato de email inválido.
- ¿Qué ocurre si la oportunidad expira exactamente en el instante de la consulta (remainingMinutes = 0)? El sistema muestra la oportunidad como "expired" o con 0 minutos restantes según el estado ya persistido.
- ¿Qué ocurre si el comprador tiene una inscripción con status "consumed" o "expired" (sin oportunidad previa o con oportunidad ya procesada)? El sistema devuelve los datos de inscripción con su estado actual y la oportunidad asociada si existe.
- ¿Qué ocurre con inyección de caracteres especiales en el parámetro email? El sistema valida el formato antes de consultar y rechaza con 400.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema DEBE exponer un endpoint `GET /api/waitlist/entries` que acepte los parámetros de consulta `eventId` (numérico) y `email` (cadena de texto).
- **FR-002**: El sistema DEBE devolver código 200 con los datos de inscripción y oportunidad asociada cuando existe una inscripción que coincida con el eventId y email proporcionados.
- **FR-003**: El sistema DEBE devolver código 404 cuando no exista ninguna inscripción que coincida con los parámetros proporcionados.
- **FR-004**: El sistema DEBE validar que ambos parámetros (`eventId` y `email`) estén presentes y devolver código 400 si alguno falta o está vacío.
- **FR-005**: El sistema DEBE validar el formato del email (RFC 5321 simplificado) y devolver código 400 si el formato es inválido.
- **FR-006**: Cuando la inscripción tiene una oportunidad activa, la respuesta DEBE incluir los minutos restantes de validez calculados dinámicamente (diferencia entre la fecha de expiración y el momento actual de la consulta).
- **FR-007**: Cuando la oportunidad está activa, la respuesta DEBE indicar que una entrada está temporalmente reservada para el comprador.
- **FR-008**: Cuando la oportunidad ha expirado, la respuesta NO DEBE mostrar información de reserva ni tiempo restante.
- **FR-009**: La respuesta DEBE incluir los datos completos de la inscripción: identificador, eventId, email del comprador, estado de la inscripción y fecha de inscripción.
- **FR-010**: Cuando existe oportunidad asociada, la respuesta DEBE incluir: identificador de oportunidad, identificador de entrada, estado de la oportunidad, fecha de activación y fecha de expiración.
- **FR-011**: La operación DEBE ser de solo lectura (no modifica estado en el sistema).
- **FR-012**: El sistema DEBE buscar la inscripción más reciente cuando existan múltiples inscripciones históricas (escenarios de reinscripción tras consumo o expiración previos).

### Key Entities

- **Inscripción en lista de espera (WaitlistEntry)**: Representa la intención de un comprador de adquirir una entrada para un evento específico. Atributos clave: identificador, evento asociado, email del comprador, estado (active/consumed/expired), fecha de inscripción.
- **Oportunidad de compra (WaitlistOpportunity)**: Representa una ventana temporal asignada a un inscrito para que pueda adquirir una entrada. Atributos clave: identificador, inscripción asociada, entrada temporalmente reservada, estado (active/consumed/expired), fecha de activación, fecha de expiración.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El comprador puede consultar su estado de inscripción en menos de 2 segundos desde que envía la petición.
- **SC-002**: El sistema responde correctamente a los cuatro estados posibles del comprador: inscripción activa (en espera), oportunidad activa (entrada reservada con tiempo restante), oportunidad consumida (avanzó al pago), oportunidad expirada (tiempo agotado).
- **SC-003**: Los minutos restantes mostrados al comprador con oportunidad activa son precisos con un margen de error menor a 1 minuto respecto al tiempo real de expiración.
- **SC-004**: El 100% de las consultas con parámetros inválidos o inexistentes devuelven códigos de error apropiados (400 o 404) con mensajes descriptivos.
- **SC-005**: La consulta no genera efectos secundarios en el sistema (no altera estados, no crea registros).

## Assumptions

- La feature de inscripción en lista de espera (001-waitlist-enrollment) ya está implementada y operativa, incluyendo el modelo de datos de `WaitlistEntry` en la base de datos.
- La entidad `WaitlistOpportunity` será creada como parte de la feature de asignación de oportunidades (Feature1). Esta feature de consulta lee datos que serán escritos por esa feature; si la oportunidad aún no existe en el modelo, la respuesta simplemente devuelve oportunidad nula.
- La consulta se ejecuta de forma síncrona en el CrudService, sin interacción con RabbitMQ ni con otros microservicios.
- No se requiere autenticación para esta consulta en el MVP — el email actúa como identificador del comprador (consistente con el diseño de 001-waitlist-enrollment).
- El cálculo de minutos restantes se realiza en tiempo de consulta (server-side) a partir de la fecha de expiración almacenada en la oportunidad.
- El endpoint responde con la inscripción más reciente del comprador para ese evento, lo que permite manejar escenarios de reinscripción de forma transparente.
