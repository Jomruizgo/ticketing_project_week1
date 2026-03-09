# Esquema de Base de Datos TicketRush

## Fuente de verdad

La fuente de verdad inicial del esquema es `scripts/schema.sql`.

## Tipos enum PostgreSQL

### `ticket_status`

- `available`
- `reserved`
- `paid`
- `released`
- `cancelled`

### `payment_status`

- `pending`
- `approved`
- `failed`
- `expired`

## Tablas principales

### `events`

- `id` BIGSERIAL PK
- `name` VARCHAR(200) NOT NULL
- `starts_at` TIMESTAMPTZ NOT NULL

### `tickets`

- `id` BIGSERIAL PK
- `event_id` BIGINT FK → `events.id`
- `status` `ticket_status` NOT NULL DEFAULT `available`
- `reserved_at` TIMESTAMPTZ NULL
- `expires_at` TIMESTAMPTZ NULL
- `paid_at` TIMESTAMPTZ NULL
- `order_id` VARCHAR(80) NULL
- `reserved_by` VARCHAR(120) NULL
- `version` INT NOT NULL DEFAULT `0`

Restricción clave:

- Si `status = reserved`, entonces `reserved_at` y `expires_at` deben existir.

### `payments`

- `id` BIGSERIAL PK
- `ticket_id` BIGINT FK → `tickets.id`
- `status` `payment_status` NOT NULL DEFAULT `pending`
- `provider_ref` VARCHAR(120) NULL
- `amount_cents` INT NOT NULL CHECK `> 0`
- `currency` CHAR(3) NOT NULL DEFAULT `USD`
- `created_at` TIMESTAMPTZ NOT NULL DEFAULT `NOW()`
- `updated_at` TIMESTAMPTZ NOT NULL DEFAULT `NOW()`

### `ticket_history`

- `id` BIGSERIAL PK
- `ticket_id` BIGINT FK → `tickets.id`
- `old_status` `ticket_status` NOT NULL
- `new_status` `ticket_status` NOT NULL
- `changed_at` TIMESTAMPTZ NOT NULL DEFAULT `NOW()`
- `reason` VARCHAR(200) NULL

## Índices relevantes

- `idx_tickets_status_expires_at`
- `idx_tickets_event_id`
- `idx_payments_ticket_id`
- `idx_payments_status`

## Reglas operativas derivadas del esquema

1. El estado del ticket es la referencia principal para confirmar reserva y pago.
2. `version` es parte crítica del control de concurrencia de reservas.
3. `ticket_history` debe conservar trazabilidad de transiciones relevantes.
4. Cualquier cambio de estado o columna compartida obliga a revisar mappings en servicios .NET y el frontend consumidor.