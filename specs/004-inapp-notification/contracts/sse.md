# Contrato SSE: Notificación In-App de Lista de Espera

**Feature**: 004-inapp-notification  
**Date**: 2026-04-07

---

## Endpoint

### GET /api/waitlist/stream

Canal SSE (Server-Sent Events) para notificaciones en tiempo real de lista de espera.

**Query parameters:**

| Parámetro | Tipo | Requerido | Validación | Descripción |
|-----------|------|-----------|------------|-------------|
| `email` | string | Sí | Formato de email válido (FR-012) | Correo del comprador |

**Headers de respuesta (éxito):**

| Header | Valor |
|--------|-------|
| Content-Type | `text/event-stream` |
| Cache-Control | `no-cache` |
| Connection | `keep-alive` |

**Respuestas de error:**

| Código | Condición | Body |
|--------|-----------|------|
| 400 Bad Request | `email` ausente o formato inválido | `{ "detail": "A valid email address is required." }` |
| 429 Too Many Requests | Límite de conexiones SSE por email alcanzado | `{ "detail": "Too many SSE connections for this email." }` |

---

## Eventos SSE emitidos

### opportunity_activated

**Cuándo**: Se creó una oportunidad activa para el comprador.

**Formato SSE:**

```
event: opportunity_activated
data: {"opportunityId":5,"ticketId":100,"eventId":42,"expiresAt":"2026-04-07T11:15:00Z","remainingMinutes":15}

```

**Campos del payload:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| opportunityId | long | ID de la oportunidad |
| ticketId | long | ID del ticket reservado |
| eventId | long | ID del evento |
| expiresAt | string (ISO 8601) | Momento de expiración |
| remainingMinutes | int | Minutos restantes desde la activación (≤ 15 por defecto) |

### opportunity_expired

**Cuándo**: La oportunidad del comprador expiró.

**Formato SSE:**

```
event: opportunity_expired
data: {"opportunityId":5,"eventId":42,"reason":"timeout"}

```

**Campos del payload:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| opportunityId | long | ID de la oportunidad |
| eventId | long | ID del evento |
| reason | string | Motivo de la expiración (`"timeout"`) |

### Keep-alive (comentario)

**Cuándo**: Cada `SSE_KEEPALIVE_INTERVAL_SECONDS` segundos (default 30).

**Formato SSE:**

```
: keepalive

```

---

## Contrato de mensajería RabbitMQ (consumo)

### Cola consumida

| Propiedad | Valor |
|-----------|-------|
| Exchange | `tickets` |
| Routing key | `waitlist.opportunity.activated` |
| Cola | Auto-generada por instancia (`q.waitlist.sse.{instance-id}`), `autoDelete: true` |
| Tipo | Exclusiva por instancia (patrón fanout vía topic exchange) |

### Payload consumido

Mismo payload que publica `OpportunityActivatedObserver` (HU3):

```json
{
  "opportunityId": 5,
  "waitlistEntryId": 1,
  "ticketId": 100,
  "eventId": 42,
  "buyerEmail": "comprador@ejemplo.com",
  "activatedAt": "2026-04-07T11:00:00Z",
  "expiresAt": "2026-04-07T11:15:00Z"
}
```

El consumer extrae `buyerEmail` para enrutar al hub SSE y construye el payload SSE (sin `buyerEmail` ni `waitlistEntryId` ni `activatedAt` — esos campos no se exponen al cliente).

### Cola consumida (expiración)

| Propiedad | Valor |
|-----------|-------|
| Exchange | `tickets` |
| Routing key | `waitlist.opportunity.expired` |
| Cola | Auto-generada por instancia (`q.waitlist.sse.{instance-id}`), misma cola con binding adicional |
| Origen | Cola `q.waitlist.opportunity.delay` (TTL + DLX) → `q.waitlist.opportunity.expired` → consumer HU6 procesa expiración → publica notificación que llega aquí |

### Payload consumido — `waitlist.opportunity.expired`

Mismo mensaje que fue publicado al delay queue al activar la oportunidad (HU3), reenviado por DLX tras expirar el TTL. El consumer de HU6 (`WaitlistOpportunityExpiredConsumer`) puede publicar un evento de notificación dedicado, o el `SseNotificationConsumer` puede escuchar directamente la routing key `waitlist.opportunity.expired`. El payload esperado es:

```json
{
  "opportunityId": 5,
  "waitlistEntryId": 1,
  "eventId": 42,
  "buyerEmail": "comprador@ejemplo.com",
  "reason": "timeout"
}
```

El consumer extrae `buyerEmail` para enrutar al hub SSE y construye el payload SSE `opportunity_expired` (sin `buyerEmail` ni `waitlistEntryId` — esos campos no se exponen al cliente).

> **Nota**: Este contrato se confirmará cuando se implemente HU6 (spec expiración). Si el payload de HU6 difiere, actualizar esta sección antes de implementar T026.

---

## Variables de entorno

| Variable | Tipo | Default | Descripción |
|----------|------|---------|-------------|
| `SSE_KEEPALIVE_INTERVAL_SECONDS` | int | 30 | Intervalo de keep-alive en segundos |
| `SSE_MAX_CONNECTIONS_PER_EMAIL` | int | 5 | Máximo de conexiones SSE concurrentes por email |

---

## Coherencia con API_CONTRACTS.md

Este contrato es una expansión detallada de la sección `GET /api/waitlist/stream` definida en `docs/Features/Feature1/API_CONTRACTS.md`. Los payloads son idénticos. Se agregan: validación de email (400), límite de conexiones (429), keep-alive, y contrato de mensajería RabbitMQ.
