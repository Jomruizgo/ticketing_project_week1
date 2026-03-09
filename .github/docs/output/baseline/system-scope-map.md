# Mapa de Alcance del Sistema — TicketRush

## 1. Vista general por bounded context operativo

| Contexto / módulo | Responsabilidad principal | Interfaces | Estado del alcance |
|---|---|---|---|
| Frontend | UI de consulta, compra y seguimiento de estado | HTTP + SSE | Confirmado |
| CRUD Service | Lectura/administración de eventos y tickets, salida SSE | HTTP + RabbitMQ consumer | Confirmado |
| Producer | Recepción de comandos asíncronos y publicación en broker | HTTP + RabbitMQ publisher | Confirmado |
| ReservationService | Confirmación de reserva y control de concurrencia | RabbitMQ + PostgreSQL | Confirmado |
| PaymentService | Validación de pago, idempotencia, TTL y transiciones de estado | RabbitMQ + PostgreSQL | Confirmado |
| PostgreSQL | Estado persistente del negocio | SQL | Confirmado |
| RabbitMQ | Intercambio de eventos entre servicios | AMQP | Confirmado |
| Scripts operativos | Setup broker, verificación E2E, expiración | Shell | Confirmado |

---

## 2. Mapa de flujos críticos

### FLUJO-001 — Consulta y administración de eventos

- **Objetivo:** visualizar y administrar eventos.
- **Entrada principal:** frontend → `crud_service`.
- **Salida observable:** eventos creados/actualizados/eliminados y visibles para el cliente.
- **Confianza:** Alta.

### FLUJO-002 — Consulta y administración de tickets

- **Objetivo:** listar tickets por evento y crear tickets en lote.
- **Entrada principal:** frontend → `crud_service`.
- **Salida observable:** inventario disponible por evento.
- **Confianza:** Alta.

### FLUJO-003 — Reserva asíncrona de ticket

- **Objetivo:** aceptar una solicitud de reserva y materializar el cambio de `available` a `reserved`.
- **Ruta principal:** frontend → producer → RabbitMQ → ReservationService → PostgreSQL.
- **Controles clave:** validación request, `202 Accepted`, optimistic locking.
- **Confianza:** Alta.

### FLUJO-004 — Procesamiento asíncrono de pago

- **Objetivo:** aceptar la solicitud y decidir el estado final del ticket.
- **Ruta principal:** frontend → producer → RabbitMQ → paymentService → PostgreSQL.
- **Controles clave:** validación estado actual, idempotencia, TTL, transición transaccional.
- **Confianza:** Alta.

### FLUJO-005 — Notificación de estado al frontend

- **Objetivo:** informar el estado final del ticket al usuario.
- **Ruta principal:** worker → `ticket.status.changed` → `crud_service` → SSE → frontend.
- **Controles clave:** contrato JSON, aislamiento por ticket, parsing/formatting SSE.
- **Confianza:** Alta.

### FLUJO-006 — Liberación de ticket por rechazo o expiración

- **Objetivo:** devolver el ticket a un estado reutilizable cuando corresponde.
- **Rutas observadas:**
  - rechazo de pago → `released`,
  - expiración por TTL tardío en payment validation,
  - expiración automática canónica vía evento `ticket.expired` en RabbitMQ.
- **Confianza:** Alta.

---

## 3. Mapa de contratos principales

### HTTP síncrono (`crud_service`)

- eventos: consulta/creación/actualización/eliminación,
- tickets: consulta individual, consulta por evento, creación bulk, actualización,
- stream SSE por ticket.

### HTTP asíncrono (`producer`)

- `POST /api/tickets/reserve` → aceptación del comando de reserva,
- `POST /api/payments/process` → aceptación del comando de pago.

### RabbitMQ

- `ticket.reserved`
- `ticket.payment.requested`
- `ticket.payments.approved`
- `ticket.payments.rejected`
- `ticket.status.changed`
- evidencia adicional de `ticket.expired`

### Persistencia

- `events`
- `tickets`
- `payments`
- `ticket_history`

---

## 4. Mapa de riesgos por frontera

| Frontera | Riesgo principal | Severidad |
|---|---|---|
| frontend ↔ producer | confundir `202 Accepted` con éxito final | Alta |
| producer ↔ RabbitMQ | publicación correcta y routing keys consistentes | Alta |
| ReservationService ↔ DB | doble reserva bajo concurrencia | Crítica |
| paymentService ↔ DB | inconsistencias por eventos duplicados o TTL tardío | Crítica |
| workers ↔ crud_service | pérdida de `ticket.status.changed` | Alta |
| crud_service ↔ frontend SSE | ruptura silenciosa del contrato del stream | Alta |

---

## 5. Fuera de alcance confirmado o no probado como baseline canónico

Estos elementos no deben asumirse como requisitos consolidados sin validación adicional:

- autenticación/autorización formal del usuario final,
- reglas de negocio avanzadas de pricing o seating,
- notificaciones multicanal fuera de SSE,
- compensaciones distribuidas complejas más allá de release/retry actual,
- auditoría completa de backlog histórico por HU.
