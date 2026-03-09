# Baseline de Requisitos del Proyecto — TicketRush

## 1. Resumen ejecutivo

Este documento reconstruye la línea base funcional y técnica del sistema **a partir de evidencia del repositorio**, no de un backlog centralizado.

### Conclusión operativa

El proyecto implementa un sistema distribuido de ticketing con estos bloques principales:

- frontend web para consulta y compra,
- API síncrona para lectura/administración (`crud_service`),
- API asíncrona de comandos (`producer`),
- worker de reservas (`ReservationService`),
- worker de pagos (`paymentService`),
- PostgreSQL como persistencia,
- RabbitMQ como backbone de mensajería,
- SSE para notificación de cambios de estado hacia el frontend.

### Nivel de confianza del baseline

- **Alto** en flujos principales de eventos, tickets, reserva, pago y notificación de estado.
- **Alto** en el mecanismo canónico de expiración automática, aclarado como flujo oficial basado en RabbitMQ.
- **Medio** en trazabilidad exacta a HUs originales, porque no existe una fuente formal, única y vigente de requerimientos históricos.

### Criterio de clasificación usado

- **Confirmado**: comportamiento respaldado por código, tests, scripts o contratos vigentes.
- **Inferido**: deducido razonablemente de varias fuentes, sin una única evidencia canónica.
- **Pendiente de validar**: hay contradicción, ambigüedad o evidencia incompleta.

---

## 2. Alcance funcional actual del producto

### 2.1 Capacidades de negocio visibles

1. Gestión de eventos.
2. Gestión de tickets por evento.
3. Reserva asíncrona de tickets.
4. Solicitud asíncrona de pago.
5. Confirmación o rechazo de pago.
6. Cambio y consulta de estado del ticket.
7. Notificación push del estado vía SSE.
8. Liberación de tickets por rechazo o expiración.
9. Registro histórico de cambios de estado.

### 2.2 Arquitectura funcional observable

- El frontend consulta lectura/administración en `crud_service`.
- El frontend envía comandos asíncronos a `producer`.
- `producer` devuelve `202 Accepted` y publica eventos en RabbitMQ.
- Los workers consumen y materializan cambios de estado en PostgreSQL.
- `crud_service` consume `ticket.status.changed` y empuja el resultado al frontend mediante SSE.

---

## 3. Requisitos funcionales confirmados

| ID | Requisito | Estado | Evidencia principal | Componentes afectados | Riesgo si se interpreta mal |
|---|---|---|---|---|---|
| BR-F-001 | El sistema debe permitir listar eventos existentes. | Confirmado | `README.md`, `frontend/lib/api.ts` (`getEvents`, `getEvent`) | frontend, crud_service | Bajo |
| BR-F-002 | El sistema debe permitir crear, actualizar y eliminar eventos desde la API síncrona. | Confirmado | `frontend/lib/api.ts` (`createEvent`, `updateEvent`, `deleteEvent`) | frontend, crud_service | Medio |
| BR-F-003 | El sistema debe permitir listar tickets por evento y consultar ticket individual. | Confirmado | `frontend/lib/api.ts` (`getTicketsByEvent`, `getTicket`) | frontend, crud_service | Bajo |
| BR-F-004 | El sistema debe permitir crear tickets en lote. | Confirmado | `frontend/lib/api.ts` (`createTickets`) | frontend, crud_service | Medio |
| BR-F-005 | La reserva de ticket debe ser un comando asíncrono aceptado por HTTP y procesado vía RabbitMQ. | Confirmado | `producer/.../TicketsController.cs`, `compose.yml`, `scripts/setup-rabbitmq.sh` | producer, RabbitMQ, ReservationService | Alto |
| BR-F-006 | El endpoint de reserva debe validar request y responder `202 Accepted` cuando el mensaje es aceptado. | Confirmado | `producer/.../TicketsController.cs` | producer | Alto |
| BR-F-007 | La reserva debe aplicar control de concurrencia optimista para evitar dobles reservas. | Confirmado | `ReservationService.../TicketRepository.cs` (`status + version`) | ReservationService, PostgreSQL | Crítico |
| BR-F-008 | El pago debe solicitarse como comando asíncrono y publicarse en RabbitMQ. | Confirmado | `producer/.../PaymentsController.cs`, `frontend/lib/api.ts`, `compose.yml` | frontend, producer, RabbitMQ | Alto |
| BR-F-009 | La aprobación de pago solo debe procesarse si el ticket está `reserved`. | Confirmado | `PaymentValidationService.cs` | paymentService | Alto |
| BR-F-010 | El sistema debe tratar eventos duplicados de pago aprobado o rechazado de forma idempotente. | Confirmado | `PaymentValidationService.cs`, `TicketStateService.cs` | paymentService | Alto |
| BR-F-011 | Si el pago llega después del TTL válido, el ticket debe liberarse. | Confirmado | `PaymentValidationService.cs` (`TTL exceeded`) | paymentService | Alto |
| BR-F-012 | Si el pago se aprueba dentro de plazo, el ticket debe pasar a `paid`. | Confirmado | `TicketStateService.cs` | paymentService, PostgreSQL | Alto |
| BR-F-013 | Si el pago se rechaza, el ticket debe pasar a `released`. | Confirmado | `PaymentValidationService.cs`, `TicketStateService.cs` | paymentService, PostgreSQL | Alto |
| BR-F-014 | Los cambios de estado del ticket deben notificarse por evento `ticket.status.changed`. | Confirmado | `README.md`, `scripts/setup-rabbitmq.sh`, pruebas de SSE documentadas | ReservationService, paymentService, crud_service | Alto |
| BR-F-015 | El frontend debe poder esperar confirmación del estado final del ticket usando SSE como mecanismo canónico; el polling residual debe tratarse como legado/desalineación. | Confirmado | hooks SSE del frontend + aclaración funcional del proyecto; existencia de `use-reservation-status.ts` como remanente no canónico | frontend | Alto |
| BR-F-016 | Debe existir un stream SSE por ticket para notificar cambios de estado al cliente. | Confirmado | `README.md`, `crud_service` tests documentados, hooks SSE del frontend | frontend, crud_service | Alto |
| BR-F-017 | El sistema debe registrar historial de cambios de estado del ticket. | Confirmado | `schema.sql` (`ticket_history`), `TicketStateService.cs` (`RecordHistoryAsync`) | paymentService, PostgreSQL | Medio |
| BR-F-018 | El sistema debe exponer health checks básicos para APIs HTTP principales. | Confirmado | `TicketsController.cs`, `compose.yml`, `README.md` | producer, crud_service | Bajo |

---

## 4. Requisitos no funcionales confirmados

| ID | Requisito | Estado | Evidencia principal | Componentes afectados | Riesgo |
|---|---|---|---|---|---|
| BR-NF-001 | La arquitectura debe separar lectura síncrona de comandos asíncronos. | Confirmado | `README.md`, `compose.yml`, `frontend/lib/api.ts` | sistema completo | Alto |
| BR-NF-002 | RabbitMQ debe ser la infraestructura de mensajería principal con exchange `tickets`. | Confirmado | `compose.yml`, `scripts/setup-rabbitmq.sh` | producer, workers, crud_service | Alto |
| BR-NF-003 | PostgreSQL debe usar enums nativos para `ticket_status` y `payment_status`. | Confirmado | `scripts/schema.sql` | PostgreSQL, servicios .NET | Alto |
| BR-NF-004 | Los estados de negocio deben persistirse con semántica consistente (`available`, `reserved`, `paid`, `released`, `cancelled`; `pending`, `approved`, `failed`, `expired`). | Confirmado | `scripts/schema.sql`, código de payment/reservation | DB, workers, frontend | Alto |
| BR-NF-005 | La consistencia observable para el usuario debe basarse en confirmación de estado final, no en el `202 Accepted`. | Confirmado | `frontend/lib/api.ts`, hooks frontend, README | frontend, producer | Alto |
| BR-NF-006 | El sistema debe tolerar concurrencia e idempotencia en flujos distribuidos críticos. | Confirmado | `TicketRepository.cs`, `PaymentValidationService.cs`, `TicketStateService.cs`, estrategia de testing semana 3 | ReservationService, paymentService | Crítico |
| BR-NF-007 | Deben existir pruebas automatizadas en varios niveles para reglas críticas del sistema. | Confirmado | `TESTING_STRATEGY.md`, proyectos `*Tests.cs` | repo completo | Alto |

---

## 5. Requisitos inferidos con evidencia

| ID | Requisito inferido | Estado | Evidencia combinada | Componentes |
|---|---|---|---|---|
| BR-I-001 | El producto pretende soportar experiencia eventual-consistente donde el usuario compra sin bloquear la UI hasta la decisión final. | Inferido | `README.md`, hooks frontend, semántica `202 Accepted`, SSE | frontend, producer, workers |
| BR-I-002 | La creación y administración de eventos/tickets está pensada para una vista administrativa separada de la compra. | Inferido | estructura `frontend/app/admin`, `frontend/lib/api.ts`, README | frontend, crud_service |
| BR-I-003 | El estado `released` representa tanto rechazo de pago como liberación por expiración. | Inferido | `schema.sql`, `TicketStateService.cs`, documentación de testing | paymentService, ReservationService, frontend |
| BR-I-004 | La trazabilidad de calidad esperada del proyecto incluye TDD, evidencia `.trx` y verificación E2E reproducible. | Inferido | `AI_WORKFLOW.md`, `TESTING_STRATEGY.md` | equipo, repositorio |

---

## 6. Pendientes de validar / contradicciones detectadas

| ID | Hallazgo | Estado | Evidencia | Impacto |
|---|---|---|---|---|
| BR-P-001 | Existe desalineación residual en frontend: la reserva canónica debe confirmarse por SSE, pero persiste un hook de polling legado. | Pendiente de corregir | `frontend/hooks/use-reservation-status.ts` frente al enfoque SSE confirmado | Medio |
| BR-P-002 | Persisten referencias documentales residuales sobre expiración, aunque la implementación y la operación canónica ya quedaron alineadas a RabbitMQ. | Pendiente de corregir | `scripts/setup-rabbitmq.sh`, `ReservationService/README.md`, aclaración funcional del proyecto | Medio-Alto |
| BR-P-003 | No existe una fuente formal y única que relacione cada capacidad implementada con una HU histórica original; la trazabilidad actual es reconstruida a posteriori. | Confirmado como gap de proceso | ausencia de backlog centralizado y dependencia de docs dispersas + aclaración funcional del proyecto | Alto |

---

## 7. Recomendación explícita para el siguiente paso de QA

Con esta línea base ya es razonable construir un **plan maestro de pruebas del proyecto completo**, pero no conviene reutilizar el `QA Agent` actual sin adaptar el insumo.

### Recomendación

1. Usar este baseline como entrada primaria.
2. Derivar de aquí la matriz de flujos críticos y cobertura esperada.
3. Generar un plan maestro de pruebas a nivel sistema.
4. Definir después los checks automáticos de PR según criticidad y costo.

### Bloqueadores que deben tratarse en QA global

- depurar documentación residual para que toda la evidencia de expiración quede alineada con RabbitMQ como verdad canónica,
- eliminar o aislar el polling legado de reserva para consolidar SSE como comportamiento oficial,
- cerrar huecos de trazabilidad entre funcionalidad implementada y HU histórica mediante gobernanza documental, no solo arqueología técnica.
