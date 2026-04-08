# Data Model: Notificación In-App SSE de Lista de Espera

**Feature**: 004-inapp-notification  
**Date**: 2026-04-07

---

## Entidades nuevas

### SseClient (solo memoria, no persistido)

Representa una conexión SSE activa de un comprador.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| Id | string | Identificador único de la conexión (GUID generado al conectar) |
| Email | string | Correo electrónico del comprador (normalizado a minúsculas) |
| ResponseStream | HttpResponse | Stream de respuesta HTTP abierto |
| EventChannel | Channel\<SseEvent\> | Canal desacoplado para encolar eventos sin bloquear al productor |
| CancellationToken | CancellationToken | Token de cancelación de `HttpContext.RequestAborted` |
| ConnectedAt | DateTime | Momento de conexión (diagnóstico) |

### SseEvent (DTO en memoria)

Representa un evento a emitir por el stream SSE.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| EventType | string | Tipo de evento SSE (`opportunity_activated`, `opportunity_expired`) |
| Data | string | Payload JSON serializado |

---

## Entidades existentes (solo lectura)

### WaitlistOpportunity (consumida, no modificada)

| Campo | Tipo | Relevancia para SSE |
|-------|------|---------------------|
| Id | long | → `opportunityId` en payload |
| TicketId | long | → `ticketId` en payload de `opportunity_activated` |
| WaitlistEntryId | long | Para obtener `eventId` y `buyerEmail` vía `WaitlistEntry` |
| Status | WaitlistOpportunityStatus | Determina qué evento emitir |
| ActivatedAt | DateTime? | Para calcular `remainingMinutes` |
| ExpiresAt | DateTime? | → `expiresAt` en payload |

### WaitlistEntry (consumida vía navegación, no modificada)

| Campo | Tipo | Relevancia para SSE |
|-------|------|---------------------|
| EventId | long | → `eventId` en payload |
| BuyerEmail | string | Key de routing al hub SSE |

---

## Interfaces nuevas (Infrastructure/Sse)

### IWaitlistSseNotifier

```csharp
// Infrastructure/Sse/IWaitlistSseNotifier.cs
public interface IWaitlistSseNotifier
{
    Task SendEventAsync(string email, string eventType, string jsonPayload);
}
```

### IWaitlistSseSubscriber

```csharp
// Infrastructure/Sse/IWaitlistSseSubscriber.cs
public interface IWaitlistSseSubscriber
{
    Task<SseClient> RegisterAsync(string email, HttpResponse response, CancellationToken cancellationToken);
    Task UnregisterAsync(SseClient client);
    int GetConnectionCount(string email);
}
```

---

## Payloads SSE

### opportunity_activated

```json
{
  "opportunityId": 5,
  "ticketId": 100,
  "eventId": 42,
  "expiresAt": "2026-04-07T11:15:00Z",
  "remainingMinutes": 15
}
```

### opportunity_expired

```json
{
  "opportunityId": 5,
  "eventId": 42,
  "reason": "timeout"
}
```

---

## Relaciones

```
WaitlistSseHub (singleton, in-memory)
├── ConcurrentDictionary<email, ConcurrentBag<SseClient>>
├── implementa IWaitlistSseNotifier (ISP: producer side)
└── implementa IWaitlistSseSubscriber (ISP: consumer side)

SseNotificationConsumer (BackgroundService)
├── escucha: q.waitlist.sse.{instance-id} ← waitlist.opportunity.activated
├── despacha a: IWaitlistSseNotifier.SendEventAsync(email, "opportunity_activated", json)
└── NO depende de repositorios (solo lectura del mensaje RabbitMQ)

WaitlistController.StreamSse (acción GET)
├── valida email (FR-012)
├── verifica límite conexiones vía IWaitlistSseSubscriber.GetConnectionCount (FR-013)
├── registra vía IWaitlistSseSubscriber.RegisterAsync
├── espera RequestAborted
└── limpia vía IWaitlistSseSubscriber.UnregisterAsync
```

---

## Cambios en esquema de BD

Ninguno. Esta feature no persiste datos.

## Cambios en topología RabbitMQ

No se agregan colas permanentes a `setup-rabbitmq.sh`. Las colas SSE son auto-delete y se crean en runtime por el consumer al iniciar cada instancia del servicio.
