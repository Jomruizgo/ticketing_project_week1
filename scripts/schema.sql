-- PostgreSQL schema for ticketing system

CREATE TYPE ticket_status AS ENUM (
  'available',
  'reserved',
  'paid',
  'released',
  'cancelled'
);

CREATE TYPE payment_status AS ENUM (
  'pending',
  'approved',
  'failed',
  'expired'
);

CREATE TABLE venues (
  id BIGSERIAL PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  address VARCHAR(300)
);

CREATE TABLE events (
  id BIGSERIAL PRIMARY KEY,
  venue_id BIGINT REFERENCES venues(id),
  name VARCHAR(200) NOT NULL,
  starts_at TIMESTAMPTZ NOT NULL
);

CREATE TABLE event_sections (
  id BIGSERIAL PRIMARY KEY,
  event_id BIGINT NOT NULL REFERENCES events(id) ON DELETE CASCADE,
  name VARCHAR(100) NOT NULL,
  capacity INT NOT NULL CHECK (capacity > 0)
);

CREATE TABLE tickets (
  id BIGSERIAL PRIMARY KEY,
  event_id BIGINT NOT NULL REFERENCES events(id) ON DELETE CASCADE,
  section_id BIGINT REFERENCES event_sections(id) ON DELETE SET NULL,
  status ticket_status NOT NULL DEFAULT 'available',
  reserved_at TIMESTAMPTZ,
  expires_at TIMESTAMPTZ,
  paid_at TIMESTAMPTZ,
  order_id VARCHAR(80),
  reserved_by VARCHAR(120),
  version INT NOT NULL DEFAULT 0,
  CONSTRAINT tickets_reserved_fields
    CHECK (
      (status <> 'reserved') OR (reserved_at IS NOT NULL AND expires_at IS NOT NULL)
    )
);

CREATE TABLE payments (
  id BIGSERIAL PRIMARY KEY,
  ticket_id BIGINT NOT NULL REFERENCES tickets(id) ON DELETE CASCADE,
  status payment_status NOT NULL DEFAULT 'pending',
  provider_ref VARCHAR(120),
  amount_cents INT NOT NULL CHECK (amount_cents > 0),
  currency CHAR(3) NOT NULL DEFAULT 'USD',
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE ticket_history (
  id BIGSERIAL PRIMARY KEY,
  ticket_id BIGINT NOT NULL REFERENCES tickets(id) ON DELETE CASCADE,
  old_status ticket_status NOT NULL,
  new_status ticket_status NOT NULL,
  changed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  reason VARCHAR(200)
);

CREATE INDEX idx_tickets_status_expires_at ON tickets(status, expires_at);
CREATE INDEX idx_tickets_event_id ON tickets(event_id);
CREATE INDEX idx_payments_ticket_id ON payments(ticket_id);
CREATE INDEX idx_payments_status ON payments(status);
