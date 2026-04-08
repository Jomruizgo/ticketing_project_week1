# Feature Specification: Waitlist Opportunity Status & Claim UI

**Feature Branch**: `008-waitlist-opportunity-ui`  
**Created**: 2026-04-08  
**Status**: Draft  
**Input**: User description: "Construye la experiencia frontend de estado y acción sobre oportunidad para la feature de lista de espera."

## User Scenarios & Testing

### User Story 1 — Consulta de estado de inscripción activa sin oportunidad (Priority: P1) 🎯 MVP

Un comprador que se inscribió en la lista de espera de un evento sin disponibilidad navega a la página de compra del evento. La aplicación consulta su estado proporcionando eventId y email. El sistema confirma que su inscripción está activa pero aún no tiene oportunidad asignada. El comprador ve un mensaje indicando que está en la lista de espera, esperando que se libere una entrada.

**Why this priority**: Es el estado más frecuente para un comprador inscrito. Sin esta consulta, el comprador no puede verificar si sigue en espera ni saber que el sistema reconoce su inscripción. Es la base sobre la cual se construyen las transiciones de estado.

**Independent Test**: Inscribir un comprador (HU7), regresar a la página del evento e ingresar el mismo email en el campo de consulta. Comprobar que la aplicación muestra "Inscripción activa" sin botón de pago ni cuenta regresiva.

**Acceptance Scenarios**:

1. **Given** un comprador inscrito en la lista de espera del evento 42 sin oportunidad asignada, **When** consulta su estado desde la página de compra proporcionando su email, **Then** la aplicación muestra su inscripción como activa y un mensaje indicando que está en espera.
2. **Given** un comprador inscrito sin oportunidad, **When** consulta su estado, **Then** no se muestra botón de "Avanzar al pago" ni cuenta regresiva ni información de reserva temporal.

---

### User Story 2 — Oportunidad activa con cuenta regresiva y acción de claim (Priority: P1) 🎯 MVP

Un comprador inscrito tiene una oportunidad activa: una entrada fue reservada temporalmente para él. Al consultar su estado o al recibir una notificación SSE `opportunity_activated`, la aplicación muestra que tiene una oportunidad activa con una cuenta regresiva en minutos que indica el tiempo restante de vigencia. Un botón prominente "Avanzar al pago" permite al comprador consumir la oportunidad. Al hacer clic, la aplicación llama a POST /api/waitlist/opportunities/{id}/claim. Si la respuesta es 200, la oportunidad pasa a "consumed" y el comprador es redirigido al flujo de pago existente con el ticketId reservado.

**Why this priority**: Es el flujo de conversión de la feature. Un comprador con oportunidad activa que no puede actuar pierde la oportunidad. Es el núcleo de valor de la HU8.

**Independent Test**: Crear una oportunidad activa para un comprador, consultar el estado, verificar que la cuenta regresiva y el botón aparecen, hacer clic y comprobar la redirección al flujo de pago.

**Acceptance Scenarios**:

1. **Given** un comprador con oportunidad activa que tiene 12 minutos restantes, **When** consulta su estado, **Then** la aplicación muestra "Oportunidad activa", una cuenta regresiva en formato MM:SS (ej. 12:00) y un botón prominente "Avanzar al pago".
2. **Given** un comprador con oportunidad activa, **When** hace clic en "Avanzar al pago", **Then** la aplicación llama a POST /api/waitlist/opportunities/{id}/claim con su email; tras respuesta 200 la oportunidad pasa a "consumed" y el comprador es redirigido al flujo de pago con el ticketId reservado.
3. **Given** un comprador conectado al stream SSE con inscripción activa, **When** el sistema activa una oportunidad para él, **Then** la aplicación recibe el evento `opportunity_activated` y transiciona la UI de "en espera" a "oportunidad activa" con cuenta regresiva y botón de pago, sin recarga de página.

---

### User Story 3 — Oportunidad expirada (Priority: P2)

Un comprador cuya oportunidad venció sin ser reclamada consulta su estado o recibe una notificación SSE `opportunity_expired`. La aplicación muestra que la oportunidad expiró, sin botón de pago ni cuenta regresiva.

**Why this priority**: Cierra el ciclo de vida visible de la oportunidad. Sin este estado, un comprador que no actuó a tiempo no recibe feedback claro.

**Independent Test**: Crear una oportunidad, esperar a que expire, y comprobar que la UI muestra "Oportunidad expirada" sin acciones disponibles.

**Acceptance Scenarios**:

1. **Given** un comprador cuya oportunidad expiró, **When** consulta su estado, **Then** la aplicación muestra "Oportunidad expirada" sin botón de pago, sin cuenta regresiva y sin indicación de reserva temporal.
2. **Given** un comprador conectado al stream SSE con oportunidad activa, **When** la oportunidad expira, **Then** la aplicación recibe el evento `opportunity_expired` y actualiza la UI a "expirada" sin recarga de página.

---

### User Story 4 — Oportunidad consumida (Priority: P2)

Un comprador que ya reclamó su oportunidad y avanzó al pago consulta nuevamente su estado. La aplicación muestra que la oportunidad fue consumida.

**Why this priority**: Evita confusión si el comprador regresa a la página del evento tras haber avanzado al pago.

**Independent Test**: Consumir una oportunidad, regresar a la página del evento y consultar el estado. Debe mostrar "Oportunidad consumida" sin acción de pago.

**Acceptance Scenarios**:

1. **Given** un comprador cuya oportunidad fue consumida, **When** consulta su estado, **Then** la aplicación muestra "Oportunidad consumida" indicando que ya avanzó al pago.
2. **Given** un comprador con oportunidad consumida, **When** consulta su estado, **Then** no se muestra botón de "Avanzar al pago" ni cuenta regresiva.

---

### User Story 5 — Claim fallido por oportunidad expirada (409) (Priority: P2)

Un comprador intenta reclamar una oportunidad que acaba de expirar. El servidor responde con 409. La aplicación muestra un mensaje de expiración y actualiza la vista.

**Why this priority**: Condición de carrera inevitable entre la cuenta regresiva del cliente y la expiración en el servidor. Sin este manejo, el comprador vería un error no informativo.

**Independent Test**: Simular una respuesta 409 al intentar claim y comprobar que se muestra un mensaje de expiración.

**Acceptance Scenarios**:

1. **Given** un comprador que intenta reclamar su oportunidad, **When** el servidor responde con 409, **Then** la aplicación muestra "La oportunidad ya no está activa" y actualiza la vista al estado expirado.

---

### User Story 6 — Claim fallido por email incorrecto (403) (Priority: P3)

Un comprador intenta reclamar una oportunidad con un email que no coincide con el dueño. El servidor responde con 403.

**Why this priority**: Escenario de borde que protege la integridad de la asignación.

**Independent Test**: Simular una respuesta 403 y comprobar el mensaje de error.

**Acceptance Scenarios**:

1. **Given** un comprador que intenta reclamar una oportunidad con un email distinto al del dueño, **When** el servidor responde con 403, **Then** la aplicación muestra "Esta oportunidad no pertenece al comprador indicado".

---

### User Story 7 — Comprador no inscrito (404 en consulta) (Priority: P3)

Un comprador intenta consultar su estado sin tener inscripción. El sistema responde con 404. La aplicación muestra que no existe inscripción.

**Why this priority**: Necesario para la UX completa pero no entrega valor de negocio directo.

**Independent Test**: Consultar con un email no inscrito y comprobar que la aplicación muestra un mensaje claro.

**Acceptance Scenarios**:

1. **Given** un email que no tiene inscripción en la lista de espera del evento, **When** el comprador consulta su estado, **Then** la aplicación muestra que no existe inscripción para ese comprador.

---

### Edge Cases

- ¿Qué sucede si la conexión SSE se pierde y el comprador reconecta? Los eventos durante la desconexión no se reenvían. Al reconectarse, la aplicación consulta el estado actual vía GET para sincronizarse.
- ¿Qué sucede si el comprador tiene la página abierta en dos pestañas? Ambas conexiones SSE reciben los mismos eventos. El claim desde una pestaña actualiza el estado; la otra refleja el cambio en su próxima consulta.
- ¿Qué sucede si la cuenta regresiva llega a 0 antes de que llegue el evento SSE `opportunity_expired`? La UI muestra 0 minutos. El comprador puede aún intentar reclamar; si expiró en el servidor, recibirá 409 y la UI se actualizará.
- ¿Qué sucede con un error de red al llamar al endpoint de claim? Se muestra un error genérico y se permite reintentar.
- ¿Qué sucede si el servidor responde con un código inesperado (500, 503) en claim o consulta? Se muestra un error genérico con opción de reintento.

## Clarifications

### Session 2026-04-08

- Q: ¿La cuenta regresiva debe mostrar solo minutos (actualización cada 60s) o minutos:segundos en formato MM:SS (actualización cada segundo)? → A: Formato MM:SS con actualización cada segundo (14:32, 14:31...). La vigencia de 15 minutos requiere granularidad por segundo para transmitir urgencia y seguir el estándar UX de temporizadores de expiración cortos.

## Requirements

### Functional Requirements

- **FR-001**: La aplicación DEBE ofrecer un mecanismo para que un comprador inscrito consulte su estado en la lista de espera proporcionando su email desde la página de compra del evento.
- **FR-002**: La aplicación DEBE llamar a `GET /api/waitlist/entries?eventId={id}&email={email}` para obtener el estado actual de inscripción y oportunidad del comprador.
- **FR-003**: La aplicación DEBE distinguir y presentar cuatro estados posibles: inscripción activa (en espera), oportunidad activa (con cuenta regresiva), oportunidad consumida (avanzó al pago), oportunidad expirada (tiempo agotado).
- **FR-004**: Cuando la oportunidad está activa, la aplicación DEBE mostrar una cuenta regresiva en formato MM:SS calculada a partir de `expiresAt`, un indicador de reserva temporal y un botón prominente "Avanzar al pago".
- **FR-005**: La cuenta regresiva DEBE mostrarse en formato MM:SS y actualizarse cada segundo hasta llegar a 00:00.
- **FR-006**: Al hacer clic en "Avanzar al pago", la aplicación DEBE llamar a `POST /api/waitlist/opportunities/{id}/claim` con `{ buyerEmail }`.
- **FR-007**: Tras respuesta 200 (claim exitoso), la aplicación DEBE redirigir al comprador al flujo de pago existente con el `ticketId` de la oportunidad consumida.
- **FR-008**: Tras respuesta 409 (oportunidad ya no activa), la aplicación DEBE mostrar un mensaje de expiración y actualizar la vista al estado expirado.
- **FR-009**: Tras respuesta 403 (email no coincide), la aplicación DEBE mostrar un mensaje indicando que la oportunidad no pertenece al comprador indicado.
- **FR-010**: Tras respuesta 404 en claim (oportunidad inexistente), la aplicación DEBE mostrar un error indicando que la oportunidad no fue encontrada.
- **FR-011**: La aplicación DEBE conectarse al stream SSE `GET /api/waitlist/stream?email={email}` cuando el comprador consulta su estado proporcionando su email.
- **FR-012**: Cuando la aplicación recibe un evento SSE `opportunity_activated`, DEBE transicionar la UI al estado de oportunidad activa con cuenta regresiva y botón de pago, sin recarga de página.
- **FR-013**: Cuando la aplicación recibe un evento SSE `opportunity_expired`, DEBE transicionar la UI al estado de oportunidad expirada, sin recarga de página.
- **FR-014**: Si la consulta de estado retorna 404 (no inscrito), la aplicación DEBE mostrar que no existe inscripción para ese comprador.
- **FR-015**: El botón de "Avanzar al pago" DEBE deshabilitarse durante la ejecución del claim para prevenir envíos duplicados.
- **FR-016**: Ante errores de red o respuestas inesperadas del servidor, la aplicación DEBE mostrar un mensaje de error genérico y permitir al comprador reintentar.
- **FR-017**: Al reconectarse a SSE tras una desconexión, la aplicación DEBE consultar el estado actual vía GET para sincronizarse con el estado real del sistema.
- **FR-018**: La vista de estado DEBE integrarse en la página de compra del evento existente (`/buy/[id]`), como una sección posterior a la inscripción exitosa o accesible mediante ingreso de email.

### Key Entities

- **Inscripción en lista de espera (WaitlistEntry)**: Estado del comprador en la lista. Atributos visibles: identificador, email del comprador, estado (active/consumed/expired), fecha de inscripción.
- **Oportunidad de compra (WaitlistOpportunity)**: Ventana temporal asignada al comprador para adquirir una entrada. Atributos visibles: identificador, ticketId, estado (active/consumed/expired), fecha de activación, fecha de expiración, minutos restantes (solo cuando activa).
- **Evento SSE `opportunity_activated`**: Notificación de que una oportunidad fue activada. Payload: `{ opportunityId, ticketId, eventId, expiresAt, remainingMinutes }`.
- **Evento SSE `opportunity_expired`**: Notificación de que una oportunidad expiró. Payload: `{ opportunityId, eventId, reason }`.

## Success Criteria

### Measurable Outcomes

- **SC-001**: El comprador puede consultar su estado y ver el resultado en menos de 3 segundos desde el envío de la consulta.
- **SC-002**: Cuando una oportunidad se activa, el comprador conectado al stream SSE ve la transición a "oportunidad activa" en menos de 2 segundos, sin recarga de página.
- **SC-003**: El comprador puede completar el flujo de claim (clic en "Avanzar al pago" → redirección al flujo de pago) en menos de 5 segundos.
- **SC-004**: El 100% de los estados (inscripción activa, oportunidad activa, consumida, expirada) se presentan de forma distinguible para el comprador.
- **SC-005**: La cuenta regresiva es precisa con un margen de error menor a 5 segundos respecto al estado real del servidor, coherente con la granularidad MM:SS y la latencia de consistencia eventual (SSE + red).
- **SC-006**: El 100% de los errores de claim (409, 403, 404, red) muestran mensajes informativos y permiten reintento donde aplica.

## Assumptions

- Esta feature INCLUYE un caso de uso backend nuevo (ClaimOpportunity) en el CRUD Service — endpoint `POST /api/waitlist/opportunities/{id}/claim` — además de la UI frontend. Los endpoints `GET /api/waitlist/entries` y `GET /api/waitlist/stream` ya están implementados.
- La feature 007-waitlist-enrollment-ui (inscripción front) ya está implementada: el componente `WaitlistEnrollForm`, el tipo `WaitlistEntryDto`, la función `enrollInWaitlist()` y el renderizado condicional en `/buy/[id]` están disponibles.
- El campo `remainingMinutes` de la oportunidad activa se calcula server-side; la cuenta regresiva en el frontend se calcula localmente desde `expiresAt` con actualización cada segundo en formato MM:SS.
- No se requiere autenticación del comprador; el email es el único identificador, consistente con el modelo existente.
- La reconexión SSE ante desconexiones es responsabilidad del cliente (implementación estándar de `EventSource` con retry automático).
- La redirección al flujo de pago tras claim exitoso reutiliza el flujo existente de la página de compra (`/buy/[id]`), pasando el `ticketId` reservado al componente `PaymentForm`.
- El `CRUD_URL` base es `process.env.NEXT_PUBLIC_API_CRUD || "http://localhost:8002"`.
- La infraestructura de testing (vitest + @testing-library/react) ya está instalada y configurada en el frontend (establecida en 007-waitlist-enrollment-ui).
