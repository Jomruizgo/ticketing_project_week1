# API Contract: Waitlist Enrollment

**Feature**: 001-waitlist-enrollment  
**Date**: 2026-04-07  
**Base URL**: `/api/waitlist`

## POST /api/waitlist/entries

Inscribir a un comprador en la lista de espera de un evento.

### Request

**Content-Type**: `application/json`

```json
{
  "eventId": 42,
  "buyerEmail": "comprador@ejemplo.com"
}
```

| Campo | Tipo | Requerido | Validación |
|-------|------|-----------|------------|
| eventId | long | Sí | Debe corresponder a un evento existente |
| buyerEmail | string | Sí | Formato de email válido (contiene @, dominio válido) |

### Responses

#### 201 Created — Inscripción exitosa

```json
{
  "id": 1,
  "eventId": 42,
  "buyerEmail": "comprador@ejemplo.com",
  "status": "active",
  "enrolledAt": "2026-04-07T15:30:00Z"
}
```

| Campo | Tipo | Descripción |
|-------|------|-------------|
| id | long | Identificador de la inscripción |
| eventId | long | Identificador del evento |
| buyerEmail | string | Correo del comprador |
| status | string | Estado de la inscripción (`"active"`) |
| enrolledAt | string (ISO 8601) | Fecha y hora de inscripción |

#### 400 Bad Request — Validación de entrada fallida

Campos obligatorios faltantes o formato de email inválido.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "errors": {
    "buyerEmail": ["The BuyerEmail field is required."]
  }
}
```

#### 404 Not Found — Evento no encontrado

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Event with id 999 was not found."
}
```

#### 409 Conflict — Inscripción duplicada

Ya existe una inscripción activa del mismo comprador para el mismo evento.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "An active waitlist entry already exists for this buyer and event."
}
```

#### 422 Unprocessable Entity — Lista de espera cerrada

La fecha del evento ya fue alcanzada.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.21",
  "title": "Unprocessable Entity",
  "status": 422,
  "detail": "The waitlist for this event is closed."
}
```

## DTOs (C#)

### Request DTO

```csharp
public record EnrollInWaitlistRequest(long EventId, string BuyerEmail);
```

### Response DTO

```csharp
public class WaitlistEntryDto
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public string BuyerEmail { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime EnrolledAt { get; set; }
}
```

## Mapeo de excepciones a HTTP

| Excepción de dominio | Código HTTP | Detalle |
|---------------------|-------------|---------|
| `EventNotFoundException` | 404 | Evento no encontrado |
| `DuplicateWaitlistEntryException` | 409 | Inscripción duplicada |
| `WaitlistClosedException` | 422 | Lista cerrada |
| Validación del modelo (ASP.NET) | 400 | Campos inválidos/faltantes |
