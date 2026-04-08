# Data Model — 005-email-notification

**Date**: 2026-04-08

## Entidades nuevas

### NotificationDelivery

Registro de auditoría de un intento de envío de notificación por correo electrónico.

| Campo | Tipo C# | Tipo PostgreSQL | Nullable | Default | Notas |
|-------|---------|-----------------|----------|---------|-------|
| `Id` | `long` | `BIGSERIAL` | No | auto | PK |
| `WaitlistOpportunityId` | `long` | `BIGINT` | No | — | FK → `waitlist_opportunities.id` ON DELETE CASCADE |
| `Channel` | `string` | `VARCHAR(50)` | No | — | Siempre `"email"` en esta feature. Extensible a futuro |
| `Status` | `NotificationDeliveryStatus` | `notification_delivery_status` (enum PG) | No | `'pending'` | `pending → sent \| failed` |
| `SentAt` | `DateTime` | `TIMESTAMPTZ` | No | — | Marca de tiempo del intento (no de la entrega) |
| `FailureReason` | `string?` | `TEXT` | Sí | `NULL` | Solo si `status = failed`. Mensaje de error o `"timeout"` |

**Relaciones**: N:1 con `WaitlistOpportunity` (navegación: `WaitlistOpportunity`).

**Restricciones de negocio**:
- El status solo permite una transición: `pending → sent` o `pending → failed`.
- Tras alcanzar estado terminal (`sent`/`failed`), el registro es inmutable.
- `FailureReason` DEBE ser NULL si `status != failed`.
- El registro se crea como `pending` al iniciar el intento de envío.

### NotificationDeliveryStatus (enum)

| Valor C# | Valor PostgreSQL | Descripción |
|----------|------------------|-------------|
| `Pending` | `pending` | Intento iniciado, esperando respuesta del proveedor |
| `Sent` | `sent` | Proveedor aceptó el correo para entrega |
| `Failed` | `failed` | Error del proveedor o timeout |

## Entidades modificadas

### OpportunityActivatedEvent (record — NUEVO en Domain)

Record tipado que reemplaza `WaitlistOpportunity` como payload de `IOpportunityObserver.OnOpportunityActivatedAsync`. Definido en `Domain/Events/`.

```csharp
public record OpportunityActivatedEvent(
    long OpportunityId,
    long EventId,
    string EventName,
    string BuyerEmail,
    DateTime ActivatedAt,
    DateTime ExpiresAt);
```

**Justificación**: Tell Don't Ask. El handler resuelve `eventId → eventName` una sola vez upstream. Todos los observers reciben la información completa sin queries adicionales.

**Impacto**: Reemplaza el DTO `OpportunityActivatedEvent` de `Infrastructure/Messaging/` (que se elimina). El observer existente (`OpportunityActivatedObserver`) se adapta para recibir este record.

### IOpportunityObserver (interfaz — MODIFICADA)

```csharp
// Antes (HU3):
Task OnOpportunityActivatedAsync(WaitlistOpportunity opportunity);

// Después (HU5):
Task OnOpportunityActivatedAsync(OpportunityActivatedEvent activatedEvent);
```

### AssignOpportunityHandler (MODIFICADO)

- Inyección: `IOpportunityObserver` singular → `IEnumerable<IOpportunityObserver>`
- Notificación: construye `OpportunityActivatedEvent` record y lo pasa a cada observer en foreach

## DDL — notification_deliveries

```sql
CREATE TYPE notification_delivery_status AS ENUM (
  'pending',
  'sent',
  'failed'
);

CREATE TABLE notification_deliveries (
  id              BIGSERIAL PRIMARY KEY,
  waitlist_opportunity_id BIGINT NOT NULL
    REFERENCES waitlist_opportunities(id) ON DELETE CASCADE,
  channel         VARCHAR(50)  NOT NULL,
  status          notification_delivery_status NOT NULL DEFAULT 'pending',
  sent_at         TIMESTAMPTZ  NOT NULL,
  failure_reason  TEXT
);

CREATE INDEX idx_notification_deliveries_opportunity
  ON notification_deliveries(waitlist_opportunity_id);
```

## Diagrama de relaciones

```
waitlist_entries (1) ──── (N) waitlist_opportunities (1) ──── (N) notification_deliveries
       │                            │
       └── event_id ──── events     └── ticket_id ──── tickets
```

## Puertos nuevos (Domain/Interfaces)

### IEmailSender

```csharp
public record EmailSendResult(bool Success, string? FailureReason);

public interface IEmailSender
{
    Task<EmailSendResult> SendOpportunityNotificationAsync(
        string buyerEmail,
        string eventName,
        DateTime expiresAt);
}
```

**Retorna**: `EmailSendResult` con `Success = true` si el proveedor aceptó el correo, o `Success = false` con `FailureReason` describiendo el motivo del rechazo. En fallos técnicos (timeout, conexión) también retorna `Success = false` con `FailureReason` correspondiente (p.ej. `"timeout"`). No lanza excepciones — el adaptador captura cualquier excepción interna y la encapsula en el result.

### INotificationDeliveryRepository

```csharp
public interface INotificationDeliveryRepository
{
    Task<NotificationDelivery> AddAsync(NotificationDelivery delivery);
    Task UpdateAsync(NotificationDelivery delivery);
}
```

**Nota**: Sin métodos de consulta en esta feature. SC-006 (consulta por oportunidad) se implementará como endpoint de lectura en una feature futura o directamente vía SQL de soporte.
