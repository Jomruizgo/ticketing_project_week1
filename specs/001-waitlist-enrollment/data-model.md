# Data Model: Inscripción en Lista de Espera

**Feature**: 001-waitlist-enrollment  
**Date**: 2026-04-07

## Entidades

### WaitlistEntry (nueva)

Inscripción de un comprador en la lista de espera de un evento.

| Campo | Tipo | Restricciones | Descripción |
|-------|------|---------------|-------------|
| Id | long (BIGSERIAL) | PK, auto-generado | Identificador único |
| EventId | long (BIGINT) | FK → events(id), NOT NULL | Evento al que se inscribe |
| BuyerEmail | string (VARCHAR 255) | NOT NULL | Correo del comprador |
| Status | WaitlistEntryStatus (enum) | NOT NULL, default 'active' | Estado de la inscripción |
| EnrolledAt | DateTime (TIMESTAMPTZ) | NOT NULL, default NOW() | Fecha de inscripción |

**Nombre de tabla en BD**: `waitlist_entries` (snake_case automático vía EFCore.NamingConventions)

### WaitlistEntryStatus (nuevo enum)

Tipo PostgreSQL: `waitlist_entry_status`

| Valor C# | Valor PostgreSQL | Descripción |
|-----------|-----------------|-------------|
| Active | `active` | Inscripción vigente, comprador en espera |
| Consumed | `consumed` | La oportunidad fue utilizada por el comprador |
| Expired | `expired` | La oportunidad expiró sin ser utilizada |

### Event (existente — sin modificaciones)

Se usa `Event.StartsAt` (DateTime / TIMESTAMPTZ) para determinar si la lista de espera está abierta o cerrada. Se consulta `Event.Id` para validar existencia.

## Relaciones

```text
events 1 ──── N waitlist_entries
  (id)            (event_id)
```

- Un evento puede tener muchas inscripciones en lista de espera.
- Cada inscripción pertenece a exactamente un evento.
- La relación es referencial (FK), pero no se agrega propiedad de navegación en `Event` para evitar acoplar el agregado existente a la nueva feature.
- `WaitlistEntry` tiene propiedad de navegación `Event` para consultar `StartsAt`.

## Restricciones

### Partial Unique Index

```sql
CREATE UNIQUE INDEX idx_waitlist_entries_active_unique
ON waitlist_entries (event_id, buyer_email)
WHERE status = 'active';
```

Garantiza a nivel de BD que no existan dos inscripciones activas del mismo comprador para el mismo evento (FR-004).

### Foreign Key

```sql
CONSTRAINT fk_waitlist_entries_event
  FOREIGN KEY (event_id) REFERENCES events(id) ON DELETE CASCADE
```

## Validaciones de negocio (capa de aplicación)

1. **Existencia del evento**: `EventId` debe corresponder a un evento existente (FR-010).
2. **Lista abierta**: `Event.StartsAt > DateTime.UtcNow` — si no, la lista está cerrada (FR-005).
3. **Sin duplicado activo**: no debe existir otra `WaitlistEntry` con el mismo `EventId` + `BuyerEmail` con `Status = Active` (FR-003).
4. **Email válido**: `BuyerEmail` debe tener formato válido (FR-009).

## Transiciones de estado

```text
                    ┌──────────────┐
                    │              │
                    ▼              │ (reinscripción)
  [inscripción] → Active ─────────┘
                    │
                    ├──→ Consumed  (oportunidad utilizada — otro flujo)
                    │
                    └──→ Expired   (oportunidad expiró — otro flujo)
```

- Solo la transición `[inscripción] → Active` está en scope de esta feature.
- Las transiciones a `Consumed` y `Expired` ocurren en flujos futuros.
- Desde `Consumed` o `Expired`, el comprador puede reinscribirse (nueva fila `Active`).
