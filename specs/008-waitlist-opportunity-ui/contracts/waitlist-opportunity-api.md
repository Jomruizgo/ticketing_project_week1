# Contract: Waitlist Opportunity Status & Claim API

**Feature**: 008-waitlist-opportunity-ui
**Date**: 2026-04-08
**Tipo**: Contratos de API — backend expone, frontend consume.

## Endpoints

### GET /api/waitlist/entries (existente — ya implementado)

**Base URL**: `NEXT_PUBLIC_API_CRUD` (default: `http://localhost:8002`)

#### Request

```
GET {CRUD_URL}/api/waitlist/entries?eventId={id}&email={email}
```

#### Response 200

```json
{
  "entry": {
    "id": 1,
    "eventId": 42,
    "buyerEmail": "comprador@ejemplo.com",
    "status": "active",
    "enrolledAt": "2026-04-07T10:00:00Z"
  },
  "opportunity": {
    "id": 5,
    "ticketId": 100,
    "status": "active",
    "activatedAt": "2026-04-07T11:00:00Z",
    "expiresAt": "2026-04-07T11:15:00Z",
    "remainingMinutes": 12
  }
}
```

`opportunity` es `null` si no hay oportunidad asociada. `remainingMinutes` solo presente cuando `status === "active"`.

| Código | Acción en UI |
|--------|-------------|
| 200 | Renderizar el estado correspondiente |
| 404 | Mostrar "No existe inscripción" |

---

### POST /api/waitlist/opportunities/{id}/claim (NUEVO — implementar)

**Base URL**: `NEXT_PUBLIC_API_CRUD` (default: `http://localhost:8002`)

#### Request

```
POST {CRUD_URL}/api/waitlist/opportunities/{id}/claim
Content-Type: application/json

{
  "buyerEmail": "comprador@ejemplo.com"
}
```

#### Responses

| Código | Significado | Body | Acción en UI |
|--------|-------------|------|-------------|
| 200 OK | Oportunidad consumida | `{ "opportunityId": 5, "ticketId": 100, "eventId": 42, "status": "consumed" }` | Redirigir al flujo de pago con ticketId |
| 404 Not Found | Oportunidad no existe | Error message | Mostrar "Oportunidad no encontrada" |
| 409 Conflict | Oportunidad ya no activa (expirada o consumida) | Error message | Mostrar "La oportunidad ya no está activa" |
| 403 Forbidden | Email no coincide con dueño | Error message | Mostrar "Esta oportunidad no pertenece al comprador indicado" |

---

### GET /api/waitlist/stream (existente — ya implementado)

**Base URL**: `NEXT_PUBLIC_API_CRUD` (default: `http://localhost:8002`)

```
GET {CRUD_URL}/api/waitlist/stream?email={email}
Content-Type: text/event-stream
```

#### Eventos SSE

| Evento | Data |
|--------|------|
| `opportunity_activated` | `{ "opportunityId": 5, "ticketId": 100, "eventId": 42, "expiresAt": "...", "remainingMinutes": 15 }` |
| `opportunity_expired` | `{ "opportunityId": 5, "eventId": 42, "reason": "timeout" }` |

## Funciones API del frontend

```typescript
// En frontend/lib/api.ts
async getWaitlistStatus(eventId: number, email: string): Promise<WaitlistStatusResponse>
async claimOpportunity(opportunityId: number, buyerEmail: string): Promise<ClaimOpportunityResponse>
```

## Dependencia Backend

El endpoint POST /api/waitlist/opportunities/{id}/claim se implementa como parte de esta feature:
- Controller: `WaitlistController.ClaimOpportunity()`
- Use Case: `IClaimOpportunityUseCase` / `ClaimOpportunityHandler`
- Domain: `WaitlistOpportunity.TransitionTo(Consumed)` (existente)
