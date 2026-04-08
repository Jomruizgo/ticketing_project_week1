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

CREATE TABLE events (
  id BIGSERIAL PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  starts_at TIMESTAMPTZ NOT NULL
);

CREATE TABLE tickets (
  id BIGSERIAL PRIMARY KEY,
  event_id BIGINT NOT NULL REFERENCES events(id) ON DELETE CASCADE,
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

CREATE TYPE waitlist_entry_status AS ENUM (
  'active',
  'consumed',
  'expired'
);

CREATE TABLE waitlist_entries (
  id BIGSERIAL PRIMARY KEY,
  event_id BIGINT NOT NULL REFERENCES events(id) ON DELETE CASCADE,
  buyer_email VARCHAR(255) NOT NULL,
  status waitlist_entry_status NOT NULL DEFAULT 'active',
  enrolled_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_tickets_status_expires_at ON tickets(status, expires_at);
CREATE INDEX idx_tickets_event_id ON tickets(event_id);
CREATE INDEX idx_payments_ticket_id ON payments(ticket_id);
CREATE INDEX idx_payments_status ON payments(status);
CREATE UNIQUE INDEX idx_waitlist_entries_active_unique
  ON waitlist_entries (event_id, buyer_email)
  WHERE status = 'active';

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
  ticket_id BIGINT NOT NULL REFERENCES tickets(id) ON DELETE NO ACTION,
  status waitlist_opportunity_status NOT NULL DEFAULT 'pending',
  activated_at TIMESTAMPTZ,
  expires_at TIMESTAMPTZ,
  expired_at TIMESTAMPTZ,
  expiration_reason VARCHAR(100)
);

CREATE INDEX idx_waitlist_opportunities_entry
  ON waitlist_opportunities (waitlist_entry_id);
CREATE UNIQUE INDEX idx_waitlist_opportunities_active_ticket_unique
  ON waitlist_opportunities (ticket_id)
  WHERE status = 'active';
CREATE UNIQUE INDEX idx_waitlist_opportunities_active_entry_unique
  ON waitlist_opportunities (waitlist_entry_id)
  WHERE status = 'active';

-- HU5: Notification deliveries
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
