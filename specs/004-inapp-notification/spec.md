# Feature Specification: Notificación In-App de Lista de Espera

**Feature Branch**: `004-inapp-notification`  
**Created**: 2026-04-07  
**Status**: Draft  
**Input**: User description: "Construye una feature de notificación in-app para el sistema de lista de espera. Cuando la oportunidad de lista de espera de un comprador se activa (entrada reservada temporalmente para él), el sistema envía una notificación en tiempo real al comprador dentro de la aplicación sin requerir recarga de página. El canal de notificación usa Server-Sent Events (SSE). El comprador se conecta a un stream SSE persistente filtrado por su correo electrónico. Cuando se activa una oportunidad, el sistema emite un evento opportunity_activated que contiene: opportunityId, ticketId, eventId, expiresAt y remainingMinutes (por defecto 15 minutos). Cuando una oportunidad expira, el sistema emite un evento opportunity_expired que contiene: opportunityId, eventId y reason. El comprador ve el estado de su oportunidad actualizado en tiempo real: que una entrada está temporalmente reservada para él y cuánto tiempo le queda. Si el comprador navega a otro lugar y vuelve, consultar su estado aún muestra la oportunidad activa mientras no haya expirado. El endpoint SSE es GET /api/waitlist/stream?email={email} con content type text/event-stream."

## Clarifications

### Session 2026-04-07

- Q: ¿Cómo llega la notificación de oportunidad activada al canal SSE: adaptador in-process (segundo IOpportunityObserver) o consumer RabbitMQ dedicado? → A: Consumer RabbitMQ dedicado (Opción B). El evento llega vía cola RabbitMQ para soportar escalamiento horizontal del CRUD Service: cualquier instancia que tenga la conexión SSE del comprador puede emitir la notificación, no solo la instancia que procesó la asignación.
- Q: ¿El endpoint SSE debe validar que el parámetro email tenga formato de correo válido antes de aceptar la conexión? → A: Sí, validar formato de email y rechazar con 400 Bad Request si es inválido. Previene conexiones vacías o con basura que consumen recursos sin propósito.
- Q: ¿Debe existir un límite máximo de conexiones SSE concurrentes por email para mitigar abuso de recursos? → A: Sí, limitar a máximo N conexiones por email. N es configurable mediante variable de entorno (no hardcodeado) con un valor por defecto razonable. Las conexiones que excedan el límite se rechazan con 429.
- Q: ¿Cuál debe ser el intervalo de keep-alive para mantener las conexiones SSE vivas? → A: 30 segundos por defecto, configurable mediante variable de entorno.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Recepción de notificación de oportunidad activa en tiempo real (Priority: P1)

El comprador tiene la aplicación abierta y está suscrito al canal de notificaciones en tiempo real mediante su correo electrónico. Cuando el sistema activa una oportunidad de lista de espera para ese comprador (una entrada fue reservada temporalmente para él), la aplicación recibe la actualización automáticamente sin requerir recarga de página. El comprador ve que tiene una oportunidad activa, que una entrada está reservada temporalmente para él, y cuánto tiempo le queda de vigencia.

**Why this priority**: Sin esta historia, el comprador inscrito en la lista de espera no se entera dentro de la aplicación de que ya tiene una oportunidad activa. Es el núcleo de la feature: transformar un cambio de estado en el backend en una experiencia visible e inmediata para el comprador.

**Independent Test**: Se puede verificar conectando un cliente al stream SSE con un correo electrónico, activando una oportunidad para ese comprador en el backend, y comprobando que el cliente recibe el evento `opportunity_activated` con el payload correcto sin recargar la página.

**Acceptance Scenarios**:

1. **Given** un comprador conectado al stream SSE con su correo electrónico y una inscripción activa en la lista de espera del evento 42, **When** el sistema activa una oportunidad para ese comprador, **Then** el comprador recibe un evento `opportunity_activated` que contiene: `opportunityId`, `ticketId`, `eventId`, `expiresAt` y `remainingMinutes` (15 por defecto), sin recargar la página.
2. **Given** un comprador conectado al stream SSE, **When** el sistema activa una oportunidad para ese comprador, **Then** la información mostrada deja claro que una entrada está reservada temporalmente para él y muestra el tiempo restante de vigencia en minutos.
3. **Given** dos compradores conectados al stream SSE con correos distintos, **When** el sistema activa una oportunidad solo para el primer comprador, **Then** solo el primer comprador recibe el evento `opportunity_activated`; el segundo no recibe ningún evento.

---

### User Story 2 — Recepción de notificación de oportunidad expirada en tiempo real (Priority: P2)

El comprador tiene la aplicación abierta y una oportunidad activa que aún no ha utilizado. Cuando la oportunidad expira (se alcanza el tiempo de vigencia sin que el comprador haya avanzado al pago), la aplicación recibe la actualización automáticamente. El comprador ve que su oportunidad expiró y el motivo de la expiración.

**Why this priority**: Complementa la notificación de activación cerrando el ciclo de vida visible de la oportunidad. Sin esta historia, un comprador que no actúa a tiempo no recibe feedback inmediato de que su oportunidad ya no está vigente, lo que genera confusión.

**Independent Test**: Se puede verificar conectando un cliente al stream SSE, activando una oportunidad, esperando a que expire, y comprobando que el cliente recibe el evento `opportunity_expired` con el payload correcto.

**Acceptance Scenarios**:

1. **Given** un comprador conectado al stream SSE con una oportunidad activa vigente, **When** la oportunidad expira por vencimiento, **Then** el comprador recibe un evento `opportunity_expired` que contiene: `opportunityId`, `eventId` y `reason` (indicando el motivo de la expiración).
2. **Given** un comprador conectado al stream SSE cuya oportunidad acaba de expirar, **Then** la aplicación ya no muestra la oportunidad como activa ni el tiempo restante.

---

### User Story 3 — Consulta posterior consistente con el estado real (Priority: P2)

El comprador navega a otro lugar dentro de la aplicación y luego regresa a consultar su estado en la lista de espera. La aplicación muestra la oportunidad activa con el tiempo restante correcto, siempre que la oportunidad no haya expirado. La información mostrada es consistente con el estado real del sistema.

**Why this priority**: Garantiza que la experiencia del comprador es coherente independientemente de su patrón de navegación. Sin esta historia, un comprador que navega y vuelve podría ver información desactualizada, perdiendo confianza en el sistema.

**Independent Test**: Se puede verificar activando una oportunidad para un comprador, navegando fuera de la página, regresando, y comprobando que la consulta de estado devuelve la oportunidad activa con el tiempo restante actualizado.

**Acceptance Scenarios**:

1. **Given** un comprador con una oportunidad activa que tiene 10 minutos restantes, **When** el comprador navega a otra sección y regresa a consultar su estado, **Then** la aplicación muestra la oportunidad como activa con el tiempo restante coherente con el momento de activación (no reinicia el contador).
2. **Given** un comprador cuya oportunidad expiró mientras navegaba en otra sección, **When** el comprador regresa a consultar su estado, **Then** la aplicación muestra la oportunidad como expirada y no muestra ninguna reserva temporal activa.

---

### User Story 4 — La capa de notificación no modifica el estado de la oportunidad (Priority: P3)

El mecanismo de notificación in-app es exclusivamente de lectura. Publicar o emitir eventos al canal SSE no tiene ningún efecto sobre el estado de la oportunidad ni sobre la inscripción del comprador en la lista de espera. Si el canal de notificación falla, la oportunidad sigue su ciclo de vida normal.

**Why this priority**: Protege la integridad del dominio. Sin esta garantía, un fallo en la capa de notificación podría corromper el estado de la oportunidad o de la inscripción.

**Independent Test**: Se puede verificar invocando el mecanismo de notificación y comprobando que no se realizan operaciones de escritura sobre las entidades de dominio.

**Acceptance Scenarios**:

1. **Given** una oportunidad activa recién asignada, **When** el sistema publica el evento `opportunity_activated` al canal SSE, **Then** no se realiza ninguna operación de escritura sobre la oportunidad ni sobre la inscripción de lista de espera.
2. **Given** un fallo técnico en la publicación al canal SSE, **When** el sistema intenta notificar una oportunidad activada, **Then** la oportunidad permanece activa con su vigencia original y el fallo queda registrado para diagnóstico sin afectar el flujo de negocio.

---

### Edge Cases

- ¿Qué sucede si el comprador se desconecta del stream SSE y se reconecta? El sistema restablece la conexión SSE; los eventos emitidos durante la desconexión no se reenvían. La consulta de estado (`GET /api/waitlist/entries`) devuelve el estado actual, permitiendo al comprador recuperar la información perdida.
- ¿Qué sucede si dos pestañas del mismo comprador se conectan al stream SSE? Ambas conexiones reciben los mismos eventos. No se generan conflictos.
- ¿Qué sucede si el correo electrónico del parámetro no corresponde a ningún comprador inscrito? La conexión SSE se establece pero no se emiten eventos hasta que exista una oportunidad para ese correo.
- ¿Qué sucede si el servidor se reinicia mientras hay conexiones SSE activas? Las conexiones se cierran; el comprador debe reconectarse. La reconexión es responsabilidad del cliente (EventSource con retry automático). Al reconectarse, el comprador puede consultar su estado actual.
- ¿Qué sucede si se emite un evento SSE pero ningún comprador está conectado? El evento se descarta. No se almacena un historial de eventos SSE; la fuente oficial del estado es la consulta de estado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema DEBE exponer un endpoint SSE en `GET /api/waitlist/stream?email={email}` con content type `text/event-stream` para notificaciones en tiempo real de lista de espera.
- **FR-002**: El endpoint SSE DEBE filtrar los eventos por correo electrónico del comprador: cada conexión recibe únicamente los eventos del comprador identificado por el parámetro `email`.
- **FR-003**: Cuando se activa una oportunidad de lista de espera, el sistema DEBE emitir un evento SSE de tipo `opportunity_activated` con el payload: `{ opportunityId, ticketId, eventId, expiresAt, remainingMinutes }`.
- **FR-004**: Cuando una oportunidad de lista de espera expira, el sistema DEBE emitir un evento SSE de tipo `opportunity_expired` con el payload: `{ opportunityId, eventId, reason }`.
- **FR-005**: El valor de `remainingMinutes` DEBE calcularse como la diferencia entre `expiresAt` y el momento actual, redondeado al minuto inferior. El valor por defecto de vigencia es 15 minutos (configurable mediante `WAITLIST_OPPORTUNITY_TTL_MS`).
- **FR-006**: La capa de notificación SSE DEBE ser exclusivamente de lectura: emitir eventos al canal NO DEBE modificar el estado de la oportunidad ni de la inscripción de lista de espera.
- **FR-007**: Si la publicación al canal SSE falla, el fallo DEBE quedar registrado para diagnóstico y la oportunidad DEBE continuar su ciclo de vida normal sin interrupciones.
- **FR-008**: El comprador DEBE poder consultar su estado actual (oportunidad activa con tiempo restante, oportunidad expirada, inscripción activa sin oportunidad) en cualquier momento mediante el endpoint de consulta existente (`GET /api/waitlist/entries`), independientemente de si estaba conectado al stream SSE. La información devuelta DEBE ser consistente con el estado real del sistema.
- **FR-009**: El endpoint SSE DEBE mantener la conexión abierta (long-lived) siguiendo el protocolo SSE estándar. El sistema DEBE enviar comentarios keep-alive periódicos para mantener la conexión viva. El intervalo de keep-alive DEBE ser configurable mediante variable de entorno (`SSE_KEEPALIVE_INTERVAL_SECONDS`, valor por defecto: 30 segundos).
- **FR-010**: Múltiples conexiones SSE desde el mismo correo electrónico (por ejemplo, varias pestañas) DEBEN recibir los mismos eventos sin conflictos, hasta el límite definido en FR-013.
- **FR-011**: Los eventos SSE no se almacenan ni se reenvían a conexiones posteriores. Si el comprador se desconecta y reconecta, la reconexión empieza desde cero; el estado actual se obtiene mediante el endpoint de consulta.
- **FR-012**: El endpoint SSE DEBE validar que el parámetro `email` tenga formato de correo electrónico válido. Si el formato es inválido o el parámetro está ausente, el sistema DEBE rechazar la conexión con `400 Bad Request`.
- **FR-013**: El sistema DEBE limitar el número máximo de conexiones SSE concurrentes por correo electrónico. El límite DEBE ser configurable mediante variable de entorno (`SSE_MAX_CONNECTIONS_PER_EMAIL`, valor por defecto: 5). Las conexiones que excedan el límite DEBEN ser rechazadas con `429 Too Many Requests`.

### Key Entities

- **Conexión SSE**: Representa una conexión abierta entre el cliente y el servidor, filtrada por correo electrónico del comprador. No es una entidad persistida; existe solo en memoria durante la duración de la conexión.
- **Evento SSE (opportunity_activated)**: Mensaje emitido al stream cuando una oportunidad se activa. Payload: `{ opportunityId, ticketId, eventId, expiresAt, remainingMinutes }`.
- **Evento SSE (opportunity_expired)**: Mensaje emitido al stream cuando una oportunidad expira. Payload: `{ opportunityId, eventId, reason }`.
- **WaitlistOpportunity** *(existente)*: Entidad de dominio cuyo cambio de estado dispara la emisión de eventos SSE. Esta feature la consume de lectura, no la modifica.
- **WaitlistEntry** *(existente)*: Inscripción del comprador en la lista de espera. Esta feature la consume de lectura para el endpoint de consulta.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Cuando se activa una oportunidad para un comprador conectado al stream SSE, el comprador recibe la notificación en menos de 2 segundos desde la activación.
- **SC-002**: El 100% de los eventos `opportunity_activated` emitidos contienen los cinco campos obligatorios: `opportunityId`, `ticketId`, `eventId`, `expiresAt`, `remainingMinutes`.
- **SC-003**: El 100% de los eventos `opportunity_expired` emitidos contienen los tres campos obligatorios: `opportunityId`, `eventId`, `reason`.
- **SC-004**: Un comprador que navega fuera de la página y regresa puede consultar su estado actual y ver información coherente con el estado real del sistema en el 100% de los casos.
- **SC-005**: Un fallo en la capa de notificación SSE nunca produce un cambio de estado en la oportunidad ni en la inscripción de lista de espera.
- **SC-006**: La conexión SSE se mantiene estable durante al menos 30 minutos sin desconexiones provocadas por el servidor en condiciones normales de operación.

## Assumptions

- El endpoint de consulta de estado (`GET /api/waitlist/entries?eventId={eventId}&email={email}`) ya existe y devuelve la oportunidad asociada con `remainingMinutes` cuando está activa (implementado en spec 002).
- La oportunidad de lista de espera ya se activa mediante el flujo de HU3 (spec 003). La expiración (HU6) aún no está implementada. Esta feature construye la infraestructura SSE para ambos eventos, pero `opportunity_expired` se emitirá cuando HU6 esté disponible.
- El mecanismo de activación de HU3 ya notifica mediante `IOpportunityObserver.OnOpportunityActivatedAsync`. El adaptador existente (`OpportunityActivatedObserver`) publica a la routing key `waitlist.opportunity.activated`. Esta feature implementa un consumer RabbitMQ dedicado que escucha esa routing key desde una cola propia y emite el evento al canal SSE. Este diseño soporta escalamiento horizontal: cualquier instancia del CRUD Service que tenga la conexión SSE del comprador puede emitir la notificación.
- El contrato SSE (`GET /api/waitlist/stream?email={email}`) está definido en `docs/Features/Feature1/API_CONTRACTS.md` con los payloads exactos de `opportunity_activated` y `opportunity_expired`.
- El correo electrónico del comprador ya está normalizado a minúsculas en la inscripción (resuelto en spec 001).
- La reconexión SSE ante desconexiones es responsabilidad del cliente (implementación estándar de `EventSource` con retry automático). El servidor no almacena estado de reconexión.
- El CRUD Service (`crud_service/`) es el servicio donde se implementa el endpoint SSE y el adaptador observer, siguiendo la arquitectura hexagonal existente (Domain/Application/Infrastructure/Api).
- No se implementa autenticación para el endpoint SSE en este MVP. El correo electrónico como parámetro de query es suficiente para filtrar eventos (consistente con el modelo de identificación del comprador por correo del sistema existente).
- La topología RabbitMQ existente ya incluye la routing key `waitlist.opportunity.activated`. El adaptador observer de HU3 (`OpportunityActivatedObserver`) publica a esa routing key. Esta feature crea una cola dedicada con binding a esa routing key para que un consumer RabbitMQ la consuma y emita al canal SSE.
