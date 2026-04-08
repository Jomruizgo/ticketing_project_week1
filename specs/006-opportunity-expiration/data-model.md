# Data Model: Expiración de Oportunidad de Lista de Espera

**Feature**: 006-opportunity-expiration  
**Date**: 2026-04-08

## Entidades afectadas

### WaitlistOpportunity (existente — con cambios en BD)

| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | `long` (PK) | Identificador único |
| `WaitlistEntryId` | `long` (FK) | Referencia a la inscripción |
| `TicketId` | `long` (FK) | Referencia a la entrada reservada |
| `Status` | `waitlist_opportunity_status` (enum PG) | `pending`, `active`, `consumed`, `expired`, `failed` |
| `ActivatedAt` | `DateTime?` | Momento de activación |
| `ExpiresAt` | `DateTime?` | Momento planificado de expiración |
| `ExpiredAt` | `DateTime?` | Momento efectivo de procesamiento de la expiración (nuevo en HU6) |
| `ExpirationReason` | `string?` (VARCHAR(100)) | Motivo de expiración, e.g. `ttl_expired` (nuevo en HU6) |

**Tabla PostgreSQL**: `waitlist_opportunities` (se agregan columnas `expired_at TIMESTAMPTZ NULL` y `expiration_reason VARCHAR(100) NULL`).

**Transiciones de estado relevantes para HU6**:
- `active → expired` (via `TransitionTo(Expired)`)

### WaitlistEntry (existente — sin cambios)

| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | `long` (PK) | Identificador único |
| `EventId` | `long` (FK) | Referencia al evento |
| `BuyerEmail` | `string` | Correo del comprador |
| `Status` | `waitlist_entry_status` (enum PG) | `active`, `consumed`, `expired` |
| `EnrolledAt` | `DateTime` | Momento de inscripción |

**Relación con expiración**: El handler accede a `WaitlistEntry.EventId` para construir el `AssignOpportunityCommand` de reasignación. La inscripción ya fue marcada como `consumed` o `expired` al momento de la asignación original (HU3); no requiere cambio de estado adicional durante la expiración de la oportunidad.

## Puertos nuevos y extendidos

### IWaitlistOpportunityRepository (extendido)

```
Método nuevo: FindByIdAsync(long id) → WaitlistOpportunity?
```

Incluye eager loading de `WaitlistEntry` para acceder a `EventId` y `BuyerEmail`.

### IOpportunityObserver (extendido)

```
Método nuevo: OnOpportunityExpiredAsync(WaitlistOpportunity opportunity) → Task
```

### IInventoryReturnPort (nuevo)

```
Método: ReturnToInventoryAsync(long ticketId, long eventId) → Task
```

Puerto para publicar el evento de retorno al inventario cuando no hay comprador elegible para reasignación.

### IExpireOpportunityUseCase (nuevo)

```
Método: HandleAsync(ExpireOpportunityCommand command) → ExpireOpportunityResult
```

## Command y Result

### ExpireOpportunityCommand

```
record ExpireOpportunityCommand(long OpportunityId)
```

### ExpireOpportunityResult

```
enum ExpireOpportunityResultType { Expired, AlreadyExpired, AlreadyConsumed, NotFound }
record ExpireOpportunityResult(ExpireOpportunityResultType Type)
```

## Flujo de datos

```
q.waitlist.opportunity.expired (DLX)
    │
    ▼
WaitlistOpportunityExpiredConsumer
    │ deserializa {opportunityId}
    ▼
ExpireOpportunityHandler.HandleAsync(command)
    │
    ├── FindByIdAsync(opportunityId) → oportunidad
    ├── Validar estado active (idempotencia si expired/consumed)
    ├── TransitionTo(Expired)
    ├── UpdateAsync(oportunidad)
    ├── OnOpportunityExpiredAsync(oportunidad) → RabbitMQ → SSE
    ├── IAssignOpportunityUseCase.HandleAsync(ticketId, eventId)
    │   ├── Assigned → nueva oportunidad para siguiente comprador
    │   └── NoEligible/AllFailed → ReturnToInventoryAsync(ticketId, eventId)
    └── Retorna resultado
```
