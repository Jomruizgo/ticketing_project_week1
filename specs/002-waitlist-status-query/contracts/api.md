# API Contract: Consulta de Estado de Lista de Espera

**Feature**: 002-waitlist-status-query  
**Date**: 2026-04-07  
**Service**: CrudService (síncrono)

## GET /api/waitlist/entries

Consulta el estado de inscripción de un comprador en la lista de espera de un evento.

### Request

```http
GET /api/waitlist/entries?eventId={id}&email={email}
```

| Parámetro | Tipo | Ubicación | Obligatorio | Descripción |
|-----------|------|-----------|-------------|-------------|
| eventId | long | query string | Sí | Identificador del evento |
| email | string | query string | Sí | Correo electrónico del comprador |

### Responses

#### 200 OK — Inscripción encontrada (sin oportunidad)

```json
{
  "entry": {
    "id": 1,
    "eventId": 42,
    "buyerEmail": "comprador@ejemplo.com",
    "status": "active",
    "enrolledAt": "2026-04-07T10:00:00Z"
  },
  "opportunity": null
}
```

#### 200 OK — Inscripción con oportunidad activa

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

#### 200 OK — Inscripción con oportunidad consumida

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
    "status": "consumed",
    "activatedAt": "2026-04-07T11:00:00Z",
    "expiresAt": "2026-04-07T11:15:00Z",
    "remainingMinutes": 0
  }
}
```

#### 200 OK — Inscripción con oportunidad expirada

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
    "status": "expired",
    "activatedAt": "2026-04-07T11:00:00Z",
    "expiresAt": "2026-04-07T11:15:00Z",
    "remainingMinutes": 0
  }
}
```

#### 404 Not Found — Inscripción no encontrada

```json
{
  "detail": "No waitlist entry found for the given event and email."
}
```

#### 400 Bad Request — Parámetros faltantes o inválidos

```json
{
  "detail": "Both 'eventId' and 'email' query parameters are required."
}
```

```json
{
  "detail": "Invalid email format."
}
```

### Notas de implementación

- El email se normaliza a minúsculas antes de comparar (consistente con 001-waitlist-enrollment).
- `remainingMinutes` se calcula como `Math.Max(0, (int)(expiresAt - DateTime.UtcNow).TotalMinutes)`.
- Para oportunidades con status `consumed`, `expired` o `failed`: `remainingMinutes = 0`.
- El endpoint es de solo lectura — no modifica ningún estado en el sistema.
- Si `WaitlistOpportunity` no existe aún en BD (tabla no creada), la respuesta siempre devuelve `opportunity: null`.
