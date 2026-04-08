# Contract: Waitlist Enrollment API (consumido por frontend)

**Feature**: 007-waitlist-enrollment-ui
**Date**: 2026-04-08
**Tipo**: Contrato de API consumida — este frontend NO expone endpoints, consume los del CRUD Service.

## Endpoint consumido

### POST /api/waitlist/entries

**Base URL**: `NEXT_PUBLIC_API_CRUD` (default: `http://localhost:8002`)

#### Request

```
POST {CRUD_URL}/api/waitlist/entries
Content-Type: application/json

{
  "eventId": number,
  "buyerEmail": string
}
```

**Validación cliente (antes del envío)**:
- `buyerEmail` no vacío y con formato de email válido
- `eventId` es un número positivo (implícito: viene del parámetro de ruta)

#### Responses

| Código | Significado | Body | Acción en UI |
|--------|-------------|------|-------------|
| 201 Created | Inscripción exitosa | `WaitlistEntryDto` | Mostrar confirmación con estado activo |
| 404 Not Found | Evento no existe | Error message | Mostrar "Evento no encontrado" |
| 409 Conflict | Ya inscrito | Error message | Mostrar "Ya tienes una inscripción activa" (informativo) |
| 422 Unprocessable | Lista cerrada | Error message | Mostrar "La lista de espera ya cerró" |
| 5xx | Error del servidor | Variable | Mostrar error genérico + opción de reintento |

#### WaitlistEntryDto (respuesta 201)

```typescript
interface WaitlistEntryDto {
  id: number
  eventId: number
  buyerEmail: string
  status: string       // "active"
  enrolledAt: string   // ISO 8601
}
```

## Función API del frontend

```typescript
// En frontend/lib/api.ts
async enrollInWaitlist(eventId: number, buyerEmail: string): Promise<WaitlistEntryDto>
```

- Lanza `ApiError` con `.status` apropiado para 404, 409, 422, y otros errores.
- Retorna `WaitlistEntryDto` en caso de éxito (201).

## Dependencia del backend

Este contrato depende del endpoint implementado en HU1:
- `crud_service/src/CrudService.Api/Controllers/WaitlistController.cs`
- Request DTO: `CrudService.Application.Dtos.EnrollInWaitlistRequest`
- Response DTO: `CrudService.Application.Dtos.WaitlistEntryDto`
