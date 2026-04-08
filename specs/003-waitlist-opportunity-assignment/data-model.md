# Data Model: Asignación de Oportunidad de Lista de Espera

**Feature**: 003-waitlist-opportunity-assignment  
**Date**: 2026-04-07

---

## Entidades modificadas

### WaitlistOpportunity (existente — se extiende)

La entidad ya existe en `CrudService.Domain/Entities/WaitlistOpportunity.cs`. Se agrega el método `TransitionTo` para validar transiciones de estado.

| Campo | Tipo | Nullable | Descripción |
|-------|------|----------|-------------|
| Id | long | No | PK autoincremental |
| WaitlistEntryId | long | No | FK a `waitlist_entries.id` |
| TicketId | long | No | FK a `tickets.id` |
| Status | WaitlistOpportunityStatus | No | Estado actual (Pending, Active, Consumed, Expired, Failed) |
| ActivatedAt | DateTime? | Sí | Momento de transición a Active (null mientras Pending) |
| ExpiresAt | DateTime? | Sí | Momento de expiración (null mientras Pending; `ActivatedAt + TTL` al activar) |

**Transiciones permitidas (método `TransitionTo`)**:

```
Pending → Active    (reserva temporal exitosa)
Pending → Failed    (reserva temporal fallida)
Active  → Consumed  (comprador reclamó la oportunidad)
Active  → Expired   (TTL expiró sin reclamación)
```

Cualquier otra transición lanza `InvalidOpportunityTransitionException`.

**Navegación**: `WaitlistEntry` (required), `Ticket` (required)

### WaitlistEntry (existente — se extiende interfaz del repositorio)

Sin cambios en la entidad. Se agregan métodos al repositorio:

| Método nuevo | Descripción |
|-------------|-------------|
| `GetActiveEntriesByEventAsync(long eventId)` | Retorna todas las inscripciones activas para el evento. El handler pasa esta lista a la strategy para selección en memoria. |
| `UpdateStatusAsync(long entryId, WaitlistEntryStatus newStatus)` | Cambia el estado de la inscripción (active → consumed). |

### Ticket (existente — sin cambios en entidad)

Sin cambios. La reserva temporal opera directamente sobre la tabla `tickets` vía `ITicketReservationPort`, actualizando `status` de `released` a `reserved` con bloqueo optimista.

---

## Nuevos puertos (interfaces en Domain)

### IPrioritizationStrategy

```
WaitlistEntry? SelectNextEligible(IReadOnlyList<WaitlistEntry> activeEntries)
```

Patrón Strategy. La strategy opera en memoria sobre la lista que le pasa el handler (no consulta la BD). La implementación inicial (`FifoStrategy`) ordena por `enrolled_at` ascendente y retorna la primera. El handler carga la lista completa desde el repo (`GetActiveEntriesByEventAsync`) e itera con `foreach`, removiendo entries fallidas antes de cada nueva llamada a la strategy.

### ITicketReservationPort

```
Task<bool> TryReserveForWaitlistAsync(long ticketId, string buyerEmail)
```

Retorna `true` si la reserva temporal fue exitosa, `false` si falló (conflicto de concurrencia, ticket ya tomado). No lanza excepciones por fallo de negocio.

### IOpportunityObserver

```
Task OnOpportunityActivatedAsync(WaitlistOpportunity opportunity)
```

Patrón Observer. Invocado por el handler tras transición `Pending → Active`. El adaptador publica:
1. Evento `waitlist.opportunity.activated` al exchange `tickets`
2. Mensaje al delay queue `q.waitlist.opportunity.delay` para expiración por TTL

---

## Nuevo caso de uso

### AssignOpportunityCommand

```
record AssignOpportunityCommand(long TicketId, long EventId)
```

### AssignOpportunityResult

```
enum AssignOpportunityResultType { Assigned, NoEligible, AllFailed }
```

- `Assigned`: oportunidad creada y activada exitosamente
- `NoEligible`: no hay inscripciones activas en la lista de espera
- `AllFailed`: se intentó con todos los elegibles y todas las reservas fallaron

---

## Mensajes RabbitMQ

### TicketReleasedEvent (consumido)

**Routing key**: `ticket.released`  
**Exchange**: `tickets`

```json
{
  "ticketId": 100,
  "eventId": 42,
  "releasedAt": "2026-04-07T10:30:00Z"
}
```

### OpportunityActivatedEvent (publicado)

**Routing key**: `waitlist.opportunity.activated`  
**Exchange**: `tickets`

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

### TicketReturnedToInventoryEvent (publicado)

**Routing key**: `ticket.returned_to_inventory`  
**Exchange**: `tickets`

```json
{
  "ticketId": 100,
  "eventId": 42,
  "returnedAt": "2026-04-07T10:30:01Z"
}
```

### OpportunityDelayMessage (publicado al delay queue)

**Cola**: `q.waitlist.opportunity.delay`  
**TTL**: configurable vía `WAITLIST_OPPORTUNITY_TTL_MS` (default: 900000ms = 15min)  
**DLX routing key**: `waitlist.opportunity.expired`

```json
{
  "opportunityId": 5
}
```

---

## Restricciones de base de datos

### Índice parcial único en waitlist_opportunities

```sql
CREATE UNIQUE INDEX idx_waitlist_opportunities_active_unique
ON waitlist_opportunities (ticket_id)
WHERE status = 'active';
```

Garantiza que no existan dos oportunidades activas para el mismo ticket simultáneamente (FR-011, defensa en profundidad).

### Configuración EF Core (agregar en TicketingDbContext)

```csharp
modelBuilder.Entity<WaitlistOpportunity>()
    .HasIndex(o => o.TicketId)
    .IsUnique()
    .HasFilter("status = 'active'")
    .HasDatabaseName("idx_waitlist_opportunities_active_unique");
```
