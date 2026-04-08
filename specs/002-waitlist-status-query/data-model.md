# Data Model: Consulta de Estado de Lista de Espera

**Feature**: 002-waitlist-status-query  
**Date**: 2026-04-07

## Entidades

### WaitlistEntry (existente — de 001)

Sin modificaciones a la entidad de dominio. Se agrega un método de lectura al repositorio.

| Campo | Tipo | Restricciones | Descripción |
|-------|------|---------------|-------------|
| Id | long (BIGSERIAL) | PK, auto-generado | Identificador único |
| EventId | long (BIGINT) | FK → events(id), NOT NULL | Evento al que se inscribe |
| BuyerEmail | string (VARCHAR 255) | NOT NULL | Correo del comprador (normalizado a minúsculas) |
| Status | WaitlistEntryStatus (enum) | NOT NULL, default 'active' | Estado de la inscripción |
| EnrolledAt | DateTime (TIMESTAMPTZ) | NOT NULL, default NOW() | Fecha de inscripción |

### WaitlistOpportunity (nueva — definida para consulta)

Oportunidad de compra asignada a un inscrito de la lista de espera. Esta entidad se define a nivel de Domain para que el handler pueda proyectar los cuatro estados visibles. La tabla `waitlist_opportunities` será creada por la feature de asignación (Feature1).

| Campo | Tipo | Restricciones | Descripción |
|-------|------|---------------|-------------|
| Id | long (BIGSERIAL) | PK, auto-generado | Identificador único |
| WaitlistEntryId | long (BIGINT) | FK → waitlist_entries(id), NOT NULL | Inscripción a la que pertenece |
| TicketId | long (BIGINT) | FK → tickets(id), NOT NULL | Entrada temporalmente reservada |
| Status | WaitlistOpportunityStatus (enum) | NOT NULL, default 'pending' | Estado de la oportunidad |
| ActivatedAt | DateTime? (TIMESTAMPTZ) | nullable | Fecha de activación |
| ExpiresAt | DateTime? (TIMESTAMPTZ) | nullable | Fecha de expiración |

**Nombre de tabla en BD**: `waitlist_opportunities` (snake_case automático vía EFCore.NamingConventions)

### WaitlistOpportunityStatus (nuevo enum)

Tipo PostgreSQL: `waitlist_opportunity_status` (será creado por Feature1)

| Valor C# | Valor PostgreSQL | Descripción |
|-----------|-----------------|-------------|
| Pending | `pending` | Oportunidad creada, no activada aún |
| Active | `active` | Oportunidad en curso, ventana de compra abierta |
| Consumed | `consumed` | Comprador reclamó la oportunidad (avanzó al pago) |
| Expired | `expired` | La ventana de compra se cerró sin ser reclamada |
| Failed | `failed` | Fallo técnico en el proceso de asignación |

## Relaciones

```text
events 1 ──── N waitlist_entries 1 ──── 0..1 waitlist_opportunities
  (id)            (event_id)                    (waitlist_entry_id)
                                    
waitlist_opportunities ──── 1 tickets
        (ticket_id)            (id)
```

- Una inscripción en lista de espera puede tener a lo sumo una oportunidad activa.
- `WaitlistOpportunity` tiene propiedad de navegación `WaitlistEntry` para consultar la inscripción asociada.
- `WaitlistOpportunity` referencia un `Ticket` (la entrada temporalmente reservada).

## Extensiones a interfaces existentes

### IWaitlistEntryRepository (extensión)

```text
+ FindActiveByEventAndEmailAsync(eventId: long, buyerEmail: string) → Task<WaitlistEntry?>
```

Devuelve la inscripción activa más reciente del comprador para el evento dado, o `null` si no existe.

### IWaitlistOpportunityRepository (nueva interfaz)

```text
+ FindByWaitlistEntryIdAsync(waitlistEntryId: long) → Task<WaitlistOpportunity?>
```

Devuelve la oportunidad más reciente vinculada a una inscripción, o `null` si no existe.

## DTOs de respuesta

### WaitlistStatusResponse (nuevo)

```text
{
  entry: WaitlistEntryDto       // Siempre presente (no nulo)
  opportunity: WaitlistOpportunityDto?  // Nulo si no hay oportunidad asignada
}
```

### WaitlistOpportunityDto (nuevo)

```text
{
  id: long
  ticketId: long
  status: string           // "pending", "active", "consumed", "expired", "failed"
  activatedAt: DateTime?
  expiresAt: DateTime?
  remainingMinutes: int    // Calculado dinámicamente; 0 si no aplica o expiró
}
```

## Proyección de los cuatro estados visibles

| Estado del comprador | Condición | Respuesta |
|---------------------|-----------|-----------|
| Inscripción activa (en espera) | `entry.Status == Active` && `opportunity == null` | Entry con status "active", opportunity null |
| Oportunidad activa (entrada reservada) | `opportunity.Status == Active` && `ExpiresAt > UtcNow` | Entry + Opportunity con remainingMinutes > 0 |
| Oportunidad consumida (avanzó al pago) | `opportunity.Status == Consumed` | Entry + Opportunity con status "consumed", remainingMinutes 0 |
| Oportunidad expirada (tiempo agotado) | `opportunity.Status == Expired` | Entry + Opportunity con status "expired", remainingMinutes 0 |

## Esquema SQL (referencia — no ejecutar hasta Feature1)

```sql
-- NOTA: Este schema será aplicado cuando Feature1 implemente la asignación de oportunidades.
-- Se documenta aquí como referencia para el modelo de dominio.

CREATE TYPE waitlist_opportunity_status AS ENUM (
  'pending',
  'active',
  'consumed',
  'expired',
  'failed'
);

CREATE TABLE waitlist_opportunities (
  id BIGSERIAL PRIMARY KEY,
  waitlist_entry_id BIGINT NOT NULL REFERENCES waitlist_entries(id) ON DELETE CASCADE,
  ticket_id BIGINT NOT NULL REFERENCES tickets(id),
  status waitlist_opportunity_status NOT NULL DEFAULT 'pending',
  activated_at TIMESTAMPTZ,
  expires_at TIMESTAMPTZ
);

CREATE INDEX idx_waitlist_opportunities_entry ON waitlist_opportunities(waitlist_entry_id);
```
