# Contratos de Integración HTTP y Eventos de TicketRush

## Contratos HTTP principales

### `producer` — Reserva

- `POST /api/tickets/reserve`
- Entrada esperada:
  - `eventId`
  - `ticketId`
  - `orderId`
  - `reservedBy`
  - `expiresInSeconds`
- Respuesta esperada:
  - `202 Accepted`
  - cuerpo con mensaje y `ticketId`

### `producer` — Pago

- `POST /api/payments/process`
- Entrada esperada:
  - `ticketId`
  - `eventId`
  - `amountCents`
  - `currency`
  - `paymentBy`
  - `paymentMethodId`
  - `transactionRef` (si aplica)
- Respuesta esperada:
  - `202 Accepted`
  - cuerpo con mensaje, `ticketId`, `eventId`

### `crud_service` — Lectura/administración

Contratos de alto nivel usados por el frontend:

- `GET /api/events`
- `GET /api/events/{id}`
- `POST /api/events`
- `PUT /api/events/{id}`
- `DELETE /api/events/{id}`
- `GET /api/tickets/event/{eventId}`
- `GET /api/tickets/{id}`
- `POST /api/tickets/bulk`
- `PUT /api/tickets/{id}`

## Cliente frontend canónico

La integración HTTP del frontend debe canalizarse mediante `frontend/lib/api.ts`.

Reglas:

1. Si cambia un contrato HTTP, actualizar `frontend/lib/api.ts`.
2. Si cambia el shape de datos, revisar también `frontend/lib/types.ts` y componentes consumidores.
3. El frontend no debe inferir éxito final de un comando asíncrono a partir del `202`.

## Contratos de eventos RabbitMQ

### Exchange principal

- `tickets` (tipo `topic`)

### Routing keys principales

- `ticket.reserved`
- `ticket.payment.requested`
- `ticket.payments.approved`
- `ticket.payments.rejected`
- `ticket.status.changed`
- `ticket.expired`

### Queues principales observadas en la topología actual

- `q.ticket.reserved`
- `q.ticket.payment.requested`
- `q.ticket.payments.approved`
- `q.ticket.payments.rejected`
- `q.ticket.status.changed`
- `q.ticket.expired`
- `q.ticket.reserved.delay`

## Reglas de compatibilidad

1. Todo cambio de routing key debe reflejarse en `scripts/setup-rabbitmq.sh` y en productores/consumidores.
2. Un cambio de payload de evento requiere revisar todos los consumidores directos.
3. Los eventos asíncronos deben seguir siendo idempotentes desde la perspectiva del consumidor.