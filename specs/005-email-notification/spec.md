# Feature Specification: Notificación por Correo Electrónico de Oportunidad de Lista de Espera

**Feature Branch**: `005-email-notification`  
**Created**: 2026-04-07  
**Status**: Draft  

## Clarifications

### Session 2026-04-07

- Q: FR-004 define `status` como `sent | failed`, pero Key Entities y el esquema de BD (`bd_esquema.drawio`) definen `pending | sent | failed`. ¿Cuál es el ciclo de vida correcto? → A: Alinear con el esquema de BD: tres estados (`pending`, `sent`, `failed`). El registro se crea como `pending` al iniciar el intento y se actualiza según el resultado del proveedor.
- Q: FR-005 exige append-only, pero con `pending` el registro necesita una actualización (`pending → sent/failed`). ¿Cómo se reconcilia? → A: Relajar FR-005 a "inmutable tras estado terminal". Se permite exactamente una transición: `pending → sent` o `pending → failed`. Tras alcanzar estado terminal el registro no se modifica ni elimina. Append-only puro es sobre-ingeniería para esta tabla operativa.
- Q: El observer necesita el nombre del evento para el correo pero solo recibe `WaitlistOpportunity` (que tiene `eventId`, no `eventName`). ¿Cómo obtenerlo? → A: Opción B — Enriquecer el payload de `IOpportunityObserver` con un record tipado (`OpportunityActivatedEvent`) que incluya `EventName` ya resuelto. El publicador (handler de asignación) resuelve `eventId → eventName` una sola vez upstream y lo empuja a todos los observers. Esto sigue el principio Tell Don’t Ask, evita queries redundantes en N observers, y mejora testabilidad. Requiere refactor de la interfaz `IOpportunityObserver` (HU3) y actualizar el observer existente (`OpportunityActivatedObserver`).

**Input**: User description: "Construye una feature de notificación por correo electrónico para el sistema de lista de espera. Cuando la oportunidad de lista de espera de un comprador se activa, el sistema envía un correo electrónico a la dirección registrada del comprador. El correo informa que existe una oportunidad de compra activa, que la entrada está temporalmente reservada por 15 minutos, hace referencia al nombre del evento, indica el período de validez e instruye al comprador a verificar su estado dentro de la aplicación. El correo es explícitamente un aviso informativo: no es la fuente oficial del estado. Si el proveedor de correo devuelve un error, el sistema registra el intento fallido pero la oportunidad permanece activa independientemente. Cada intento de envío (exitoso o fallido) queda auditado en la tabla notification_deliveries con el resultado y la marca de tiempo. El canal de correo está aislado de la lógica de asignación: un fallo en la entrega del correo nunca afecta el ciclo de vida de la oportunidad. El disparador del envío es la activación de la oportunidad, no ninguna acción del comprador."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Envío de correo al activarse una oportunidad (Priority: P1) 🎯 MVP

El sistema detecta que una oportunidad de lista de espera fue activada para un comprador. Sin ninguna acción del comprador, el sistema envía automáticamente un correo electrónico a la dirección registrada informando que existe una oportunidad activa de compra, que la entrada está reservada temporalmente durante 15 minutos, el nombre del evento, el período de validez, y la instrucción de verificar el estado dentro de la aplicación. El correo deja explícito que es un aviso informativo y no la fuente oficial del estado.

**Why this priority**: Sin este correo, un comprador que no está conectado a la aplicación en el momento de la activación pierde la oportunidad sin enterarse. Es la razón de existir de esta feature.

**Independent Test**: Activar una oportunidad para un comprador con correo válido y verificar que se genera un correo con los campos obligatorios (nombre del evento, vigencia de 15 minutos, instrucción de verificar en la aplicación, disclaimer de aviso informativo).

**Acceptance Scenarios**:

1. **Given** un comprador con dirección de correo `comprador@ejemplo.com` y una oportunidad recién activada para el evento "Concierto Rock 2026", **When** el sistema procesa la activación de la oportunidad, **Then** el sistema envía un correo electrónico a `comprador@ejemplo.com` que contiene: el nombre del evento ("Concierto Rock 2026"), la indicación de que la entrada está reservada temporalmente, la vigencia de 15 minutos, la instrucción de verificar el estado dentro de la aplicación, y la aclaración de que el correo es un aviso informativo y no la fuente oficial del estado.
2. **Given** un comprador con una oportunidad activada, **When** el sistema genera el correo electrónico, **Then** el correo no contiene ningún enlace de acción directa (pago, confirmación); solo instruye al comprador a consultar la aplicación.
3. **Given** el sistema con múltiples oportunidades activadas simultáneamente para diferentes compradores, **When** el sistema procesa las activaciones, **Then** cada comprador recibe su propio correo con la información correcta de su oportunidad y evento específicos.

---

### User Story 2 — Auditoría de intentos de envío (Priority: P1)

Cada intento de envío de correo electrónico — sea exitoso o fallido — queda registrado en la tabla `notification_deliveries` con el resultado (`sent` o `failed`), la marca de tiempo, y en caso de fallo, la razón del error. Este registro permite trazabilidad completa de las notificaciones sin depender únicamente de logs efímeros.

**Why this priority**: Sin auditoría, Soporte no puede determinar si un comprador fue notificado o no. Es un requisito de trazabilidad que complementa directamente el envío.

**Independent Test**: Enviar un correo exitoso y verificar que se crea un registro en `notification_deliveries` con `status = sent` y `sent_at` con la marca de tiempo. Simular un fallo de proveedor y verificar que se crea un registro con `status = failed`, `sent_at` con la marca de tiempo, y `failure_reason` con el detalle del error.

**Acceptance Scenarios**:

1. **Given** una oportunidad activada y un correo enviado exitosamente, **When** el proveedor de correo confirma la entrega, **Then** el sistema crea un registro en `notification_deliveries` con `waitlist_opportunity_id` correcto, `channel = email`, `status = sent`, y `sent_at` con la marca de tiempo del intento.
2. **Given** una oportunidad activada y un fallo del proveedor de correo, **When** el proveedor devuelve un error, **Then** el sistema crea un registro en `notification_deliveries` con `status = failed`, `sent_at` con la marca de tiempo, y `failure_reason` con la descripción del error.
3. **Given** un registro de auditoría con estado terminal (`sent` o `failed`), **Then** el registro contiene la referencia a la oportunidad que disparó el envío (`waitlist_opportunity_id`), el canal (`email`), y no se modifica ni elimina tras alcanzar estado terminal.

---

### User Story 3 — Aislamiento del canal de correo respecto al ciclo de vida de la oportunidad (Priority: P2)

Un fallo en el envío de correo electrónico nunca afecta el estado de la oportunidad de lista de espera. Si el proveedor de correo está caído, el correo falla silenciosamente desde la perspectiva del flujo de negocio: la oportunidad permanece activa, el comprador puede seguir operando en la aplicación, y el fallo queda auditado. La lógica de asignación y la lógica de notificación están completamente desacopladas.

**Why this priority**: Protege la integridad del dominio. Sin este aislamiento, una caída del proveedor de correo podría bloquear o corromper oportunidades de compra.

**Independent Test**: Configurar un proveedor de correo que falle sistemáticamente, activar una oportunidad, y verificar que la oportunidad sigue activa con su vigencia original mientras el fallo de envío queda registrado.

**Acceptance Scenarios**:

1. **Given** una oportunidad activa recién asignada y un proveedor de correo que devuelve error 500, **When** el sistema intenta enviar el correo, **Then** la oportunidad permanece activa con su vigencia de 15 minutos intacta, y el fallo queda registrado en `notification_deliveries` con `status = failed`.
2. **Given** una excepción inesperada al comunicarse con el proveedor de correo, **When** la excepción ocurre durante el envío, **Then** la excepción es capturada y registrada; no se propaga al handler de asignación ni al observer que disparó la notificación.
3. **Given** un proveedor de correo que falla para una oportunidad pero funciona para otra, **When** se procesan ambas activaciones, **Then** la primera oportunidad permanece activa con fallo registrado, y la segunda oportunidad también permanece activa con envío exitoso registrado. Cada una es independiente.

---

### Edge Cases

- ¿Qué sucede si el correo del comprador tiene formato válido pero el buzón no existe? El proveedor de correo devuelve un bounce; el sistema registra el intento como `sent` (fue aceptado por el proveedor para entrega). La detección de bounces está fuera del alcance de esta feature.
- ¿Qué sucede si se activan múltiples oportunidades para el mismo comprador en rápida sucesión? Cada activación dispara su propio correo y su propio registro de auditoría. No se deduplican correos.
- ¿Qué sucede si el proveedor de correo tiene latencia alta pero eventualmente responde? El sistema espera la respuesta del proveedor con un timeout configurable. Si el proveedor responde exitosamente dentro del timeout, se registra como `sent`. Si el timeout expira, se registra como `failed` con razón "timeout".
- ¿Qué sucede si la tabla `notification_deliveries` no existe o la escritura falla? El fallo de auditoría se registra en logs pero no impide ni afecta la oportunidad. La auditoría es un efecto secundario observable, no un prerequisito del flujo de negocio.
- ¿Qué sucede si el nombre del evento contiene caracteres especiales o HTML? El contenido del correo se genera con el nombre del evento sanitizado para prevenir inyección de contenido malicioso en clientes de correo.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema DEBE enviar un correo electrónico al comprador cuando se activa una oportunidad de lista de espera. El disparador es la activación de la oportunidad, no una acción del comprador.
- **FR-002**: El correo electrónico DEBE contener como mínimo: el nombre del evento, la indicación de que la entrada está reservada temporalmente, la vigencia de 15 minutos, la instrucción de verificar el estado dentro de la aplicación, y la aclaración de que es un aviso informativo y no la fuente oficial del estado.
- **FR-003**: El correo electrónico DEBE enviarse a la dirección de correo asociada al comprador en el sistema (campo `buyerEmail` de la inscripción de lista de espera).
- **FR-004**: Cada intento de envío DEBE quedar registrado en la tabla `notification_deliveries` con: `waitlist_opportunity_id`, `channel` (`email`), `status` (`pending`, `sent` o `failed`), `sent_at` (marca de tiempo del intento), y `failure_reason` (solo si `status = failed`). El registro se crea inicialmente con `status = pending` al iniciar el intento y se actualiza a `sent` o `failed` según el resultado del proveedor.
- **FR-005**: Los registros de auditoría en `notification_deliveries` DEBEN ser inmutables tras alcanzar un estado terminal (`sent` o `failed`). Se permite exactamente una transición de estado: `pending → sent` o `pending → failed`. Tras alcanzar estado terminal, el registro no se modifica ni se elimina.
- **FR-006**: Un fallo en el envío del correo NO DEBE afectar el estado de la oportunidad ni de la inscripción de lista de espera. La oportunidad DEBE permanecer activa independientemente del resultado del envío.
- **FR-007**: Un fallo en el envío NO DEBE propagarse como excepción al handler de asignación ni al observer que disparó la notificación. El fallo DEBE ser capturado, registrado en `notification_deliveries`, y logueado para diagnóstico.
- **FR-008**: El correo electrónico NO DEBE contener enlaces de acción directa (pago, confirmación). Solo DEBE instruir al comprador a consultar su estado dentro de la aplicación.
- **FR-009**: El nombre del evento incluido en el correo DEBE estar sanitizado para prevenir inyección de contenido malicioso en clientes de correo.
- **FR-010**: El sistema DEBE utilizar un timeout configurable para la comunicación con el proveedor de correo. Si el timeout expira, el intento se registra como `failed` con razón "timeout".

### Key Entities

- **NotificationDelivery**: Registro de auditoría de un intento de envío de notificación. Atributos: `id` (PK), `waitlist_opportunity_id` (FK a `waitlist_opportunities`), `channel` (tipo de canal: `email`), `status` (`pending`, `sent`, `failed`), `sent_at` (marca de tiempo del intento), `failure_reason` (texto libre, solo si falló). Relación N:1 con `WaitlistOpportunity`.
- **WaitlistOpportunity** *(existente)*: Entidad de dominio cuya activación dispara el envío del correo. Esta feature la consume de lectura, no la modifica. Provee el `buyerEmail` y el `eventId` necesarios para el correo.
- **Event** *(existente)*: Provee el nombre del evento para incluir en el cuerpo del correo. Se consulta por su `id` obtenido de la oportunidad.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de las activaciones de oportunidad disparan un intento de envío de correo electrónico registrado en `notification_deliveries`.
- **SC-002**: El 100% de los registros en `notification_deliveries` contienen los cinco campos obligatorios: `waitlist_opportunity_id`, `channel`, `status`, `sent_at`, `failure_reason` (este último solo si aplica).
- **SC-003**: El 100% de los correos enviados exitosamente contienen los cinco elementos de contenido obligatorios: nombre del evento, reserva temporal, vigencia de 15 minutos, instrucción de verificar en la aplicación, disclaimer de aviso informativo.
- **SC-004**: El 0% de los fallos de envío de correo producen un cambio de estado en la oportunidad o en la inscripción de lista de espera.
- **SC-005**: Un fallo en el proveedor de correo no incrementa el tiempo de respuesta del flujo de asignación de oportunidades en más de 5 segundos (incluyendo el timeout de comunicación con el proveedor).
- **SC-006**: Soporte puede consultar el historial completo de intentos de envío para cualquier oportunidad dada, incluyendo resultado y marca de tiempo.

## Assumptions

- La oportunidad de lista de espera ya se activa mediante el flujo de HU3 (spec 003). El mecanismo de activación notifica a los observers registrados mediante `IOpportunityObserver.OnOpportunityActivatedAsync`. Esta feature implementa un nuevo `IOpportunityObserver` (`EmailNotificationObserver`) que se invoca como parte de esa cadena.
- La interfaz `IOpportunityObserver` se refactoriza para recibir un record tipado `OpportunityActivatedEvent` (definido en Domain) en vez de la entidad `WaitlistOpportunity`. El record incluye: `OpportunityId`, `EventId`, `EventName`, `BuyerEmail`, `ActivatedAt`, `ExpiresAt`. El handler de asignación resuelve `eventId → eventName` una sola vez y construye el record antes de notificar a los observers. Los observers existentes (`OpportunityActivatedObserver`) y futuros (`EmailNotificationObserver`) reciben toda la información necesaria sin hacer queries adicionales (Tell Don’t Ask).
- A diferencia de la notificación SSE (spec 004, que usa un consumer RabbitMQ dedicado para soportar escalamiento horizontal), la notificación por correo se implementa como un adaptador in-process (`IOpportunityObserver`). La razón: el correo no depende de a cuál instancia está conectado el comprador; cualquier instancia puede enviarlo. La latencia del proveedor de correo se maneja con timeout configurable.
- La tabla `notification_deliveries` ya está definida en el esquema de base de datos de la épica (`docs/Features/Feature1/drawio/bd_esquema.drawio`) con columnas: `id`, `waitlist_opportunity_id`, `channel`, `status`, `sent_at`, `failure_reason`. El DDL se crea como parte de esta feature si no existe.
- El nombre del evento se resuelve upstream en el handler de asignación (que ya tiene acceso a `IEventRepository`) y se incluye en el record `OpportunityActivatedEvent`. El `EmailNotificationObserver` lo recibe directamente sin necesitar un puerto de consulta propio.
- El proveedor de correo externo se abstrae detrás de una interfaz (`IEmailSender`) que retorna un `EmailSendResult(bool Success, string? FailureReason)`. El result encapsula tanto éxitos como fallos (incluyendo timeouts) sin lanzar excepciones — el adaptador captura cualquier excepción interna y la encapsula en el result. Esto permite implementación con Amazon SES, SendGrid, SMTP, o cualquier otro proveedor sin modificar la lógica del observer.
- No se implementan reintentos automáticos en caso de fallo. Un intento fallido queda registrado y el sistema continúa. Los reintentos automáticos son una mejora futura.
- El correo se genera en formato texto plano para simplicidad del MVP. HTML con plantillas es una mejora futura.
- La dirección de remitente (`from`) se configura mediante variable de entorno. No se personaliza por evento ni por comprador.
- El comprador se identifica con su correo electrónico; no hay sistema de usuarios con autenticación. El correo de destino es el mismo `buyerEmail` usado para la inscripción en lista de espera.
