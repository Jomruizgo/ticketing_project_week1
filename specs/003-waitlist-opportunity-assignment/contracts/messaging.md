# Contracts: Asignación de Oportunidad de Lista de Espera

**Feature**: 003-waitlist-opportunity-assignment  
**Date**: 2026-04-07

---

## Eventos RabbitMQ (contratos de mensajería)

Esta feature no expone endpoints HTTP. La interfaz pública son eventos de mensajería RabbitMQ.

### Evento consumido: `ticket.released`

| Campo | Tipo | Descripción |
|-------|------|-------------|
| **Exchange** | `tickets` | Exchange topic existente |
| **Routing key** | `ticket.released` | Publicado por ReservationService y paymentService |
| **Cola** | `q.ticket.released` | Declarada en `scripts/setup-rabbitmq.sh` |

**Payload**:

```json
{
  "ticketId": 100,
  "eventId": 42,
  "releasedAt": "2026-04-07T10:30:00Z"
}
```

| Campo | Tipo | Requerido | Validación |
|-------|------|-----------|------------|
| `ticketId` | int | Sí | Debe existir en tabla `tickets` |
| `eventId` | int | Sí | Debe existir en tabla `events` |
| `releasedAt` | datetime (UTC) | Sí | ISO 8601 |

**Comportamiento del consumer**:

| Escenario | Acción | ACK/NACK |
|-----------|--------|----------|
| Comprador elegible + reserva exitosa | Crea oportunidad active, publica `opportunity_activated` | ACK |
| Sin compradores elegibles | Publica `ticket.returned_to_inventory` | ACK |
| Comprador elegible + todos fallan reserva | Publica `ticket.returned_to_inventory` | ACK |
| Evento inexistente | Log warning, ignora | ACK |
| Ticket inexistente | Log warning, ignora | ACK |
| Lista de espera cerrada | Publica `ticket.returned_to_inventory` | ACK |
| Oportunidad activa ya existe para ticket | Ignora (idempotencia) | ACK |
| Fallo técnico inesperado | Log error | NACK (requeue: false) |

---

### Evento publicado: `waitlist.opportunity.activated`

| Campo | Tipo | Descripción |
|-------|------|-------------|
| **Exchange** | `tickets` | Exchange topic existente |
| **Routing key** | `waitlist.opportunity.activated` | Consumido por servicios de notificación (HU4/HU5) |

**Payload**:

```json
{
  "opportunityId": 5,
  "waitlistEntryId": 12,
  "ticketId": 100,
  "eventId": 42,
  "buyerEmail": "comprador@ejemplo.com",
  "activatedAt": "2026-04-07T10:30:02Z",
  "expiresAt": "2026-04-07T10:45:02Z"
}
```

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `opportunityId` | long | ID de la oportunidad creada |
| `waitlistEntryId` | long | ID de la inscripción asociada |
| `ticketId` | int | ID del ticket reservado |
| `eventId` | int | ID del evento |
| `buyerEmail` | string | Correo normalizado del comprador |
| `activatedAt` | datetime (UTC) | Momento de activación |
| `expiresAt` | datetime (UTC) | Momento de expiración (`activatedAt + TTL`) |

---

### Evento publicado: `ticket.returned_to_inventory`

| Campo | Tipo | Descripción |
|-------|------|-------------|
| **Exchange** | `tickets` | Exchange topic existente |
| **Routing key** | `ticket.returned_to_inventory` | Consumido por ReservationService |
| **Cola** | `q.ticket.returned` | Declarada en `scripts/setup-rabbitmq.sh` |

**Payload**:

```json
{
  "ticketId": 100,
  "eventId": 42,
  "returnedAt": "2026-04-07T10:30:01Z"
}
```

---

### Mensaje al delay queue: `q.waitlist.opportunity.delay`

| Campo | Tipo | Descripción |
|-------|------|-------------|
| **Cola** | `q.waitlist.opportunity.delay` | TTL configurable |
| **x-message-ttl** | `WAITLIST_OPPORTUNITY_TTL_MS` (default: 900000) | 15 minutos |
| **x-dead-letter-exchange** | `tickets` | |
| **x-dead-letter-routing-key** | `waitlist.opportunity.expired` | |
| **Cola destino DLX** | `q.waitlist.opportunity.expired` | Binding: `waitlist.opportunity.expired` |

**Payload**:

```json
{
  "opportunityId": 5
}
```

---

## Topología de colas a declarar en `scripts/setup-rabbitmq.sh`

```bash
# q.ticket.released — consumer de esta feature
rabbitmqadmin declare queue name=q.ticket.released durable=true
rabbitmqadmin declare binding source=tickets destination=q.ticket.released routing_key=ticket.released

# q.ticket.returned — consumido por ReservationService
rabbitmqadmin declare queue name=q.ticket.returned durable=true
rabbitmqadmin declare binding source=tickets destination=q.ticket.returned routing_key=ticket.returned_to_inventory

# q.waitlist.opportunity.delay — delay queue con TTL y DLX
rabbitmqadmin declare queue name=q.waitlist.opportunity.delay durable=true \
  arguments='{"x-message-ttl": 900000, "x-dead-letter-exchange": "tickets", "x-dead-letter-routing-key": "waitlist.opportunity.expired"}'

# q.waitlist.opportunity.expired — consumer de expiración (HU6)
rabbitmqadmin declare queue name=q.waitlist.opportunity.expired durable=true
rabbitmqadmin declare binding source=tickets destination=q.waitlist.opportunity.expired routing_key=waitlist.opportunity.expired
```
