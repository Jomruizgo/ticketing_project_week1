# Data Model: Waitlist Enrollment UI

**Feature**: 007-waitlist-enrollment-ui
**Date**: 2026-04-08

## Entidades

Esta feature es puramente frontend. No introduce nuevas tablas ni modifica el schema de BD. Consume entidades existentes del backend a través classes de abstracción en el cliente.

### WaitlistEntryDto (nueva — frontend only)

Representación en TypeScript del DTO que devuelve el backend al inscribirse en la lista de espera.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| id | number | Identificador único de la inscripción |
| eventId | number | ID del evento asociado |
| buyerEmail | string | Correo del comprador |
| status | string | Estado de la inscripción (e.g., "active") |
| enrolledAt | string | Fecha/hora de inscripción (ISO 8601) |

**Relación**: Se crea en respuesta a una inscripción exitosa (POST 201). No se persiste en el frontend — solo se usa para mostrar la confirmación.

**Alineación con backend**: Mapea 1:1 con `CrudService.Application.Dtos.WaitlistDtos.WaitlistEntryDto`.

### Event (existente — sin cambios)

| Campo relevante | Tipo | Uso en esta feature |
|-----------------|------|---------------------|
| id | number | Se pasa como `eventId` al endpoint de inscripción |
| availableTickets | number | Determina si mostrar flujo de compra (>0) o waitlist (===0) |
| startsAt | string | Determina si la lista de espera está abierta (fecha futura) o cerrada (fecha pasada) |

### EnrollInWaitlistPayload (nueva — frontend only)

Payload de la solicitud de inscripción.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| eventId | number | ID del evento |
| buyerEmail | string | Correo del comprador, validado en cliente |

## Validaciones

| Regla | Capa | Detalle |
|-------|------|---------|
| Email requerido y con formato válido | Frontend (cliente) | Validación HTML5 `type="email"` + regex básico antes del envío |
| Prevención de doble envío | Frontend (cliente) | Botón deshabilitado durante procesamiento (`loading` state) |
| Evento con disponibilidad | Backend (CRUD) | Si `availableTickets > 0`, no se ofrece waitlist. Si se envía de todas formas, el backend puede rechazarlo |
| Duplicado | Backend (CRUD) | 409 Conflict si ya existe inscripción activa |
| Lista cerrada | Backend (CRUD) | 422 Unprocessable si la fecha del evento pasó |

## Transiciones de estado (vista del UI)

```
[Página cargando]
        │
        ▼
   ┌────────────────┐
   │ Event loaded    │
   └────────────────┘
        │
        ├─ availableTickets > 0 ──────────► [Flujo de compra existente]
        │
        ├─ availableTickets === 0
        │   AND startsAt > now ───────────► [WaitlistEnrollForm]
        │                                       │
        │                                       ├─ Submit → 201 ──► [Confirmación: inscripción activa]
        │                                       ├─ Submit → 409 ──► [Info: ya inscrito]
        │                                       ├─ Submit → 422 ──► [Info: lista cerrada]
        │                                       ├─ Submit → 404 ──► [Error: evento no encontrado]
        │                                       └─ Submit → Error ─► [Error genérico + retry]
        │
        └─ availableTickets === 0
            AND startsAt <= now ──────────► [Mensaje: lista de espera cerrada]
```
