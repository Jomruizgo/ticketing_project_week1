-- Insert de datos de ejemplo para pruebas

-- 1. Insertar eventos
INSERT INTO events (name, starts_at) VALUES
  ('Concierto de Rock 2026', '2026-03-15T20:00:00Z'),
  ('Festival de Música Electrónica', '2026-04-10T18:00:00Z'),
  ('Conferencia de Tecnología', '2026-05-20T09:00:00Z');

-- 2. Insertar tickets disponibles (status = 'available')
INSERT INTO tickets (event_id, status, order_id, reserved_by, version) VALUES
  (1, 'available', NULL, NULL, 0),
  (1, 'available', NULL, NULL, 0),
  (1, 'available', NULL, NULL, 0),
  (1, 'available', NULL, NULL, 0),
  (1, 'available', NULL, NULL, 0),
  (2, 'available', NULL, NULL, 0),
  (2, 'available', NULL, NULL, 0),
  (2, 'available', NULL, NULL, 0),
  (3, 'available', NULL, NULL, 0),
  (3, 'available', NULL, NULL, 0);

-- 3. Insertar tickets reservados (status = 'reserved')
-- NOTA: Cuando status = 'reserved', DEBE haber reserved_at Y expires_at (CHECK constraint)
INSERT INTO tickets (event_id, status, reserved_at, expires_at, order_id, reserved_by, version) VALUES
  (1, 'reserved', NOW(), NOW() + INTERVAL '5 minutes', 'ORD-2026-001', 'usuario1@example.com', 0),
  (1, 'reserved', NOW() - INTERVAL '10 minutes', NOW() + INTERVAL '10 minutes', 'ORD-2026-002', 'usuario2@example.com', 0);

-- 4. Insertar tickets pagados (status = 'paid')
INSERT INTO tickets (event_id, status, reserved_at, paid_at, expires_at, order_id, reserved_by, version) VALUES
  (1, 'paid', NOW() - INTERVAL '30 minutes', NOW() - INTERVAL '5 minutes', NOW() + INTERVAL '1 day', 'ORD-2026-003', 'usuario3@example.com', 0),
  (2, 'paid', NOW() - INTERVAL '2 hours', NOW() - INTERVAL '1 hour', NOW() + INTERVAL '1 day', 'ORD-2026-004', 'usuario4@example.com', 0);

-- 5. Insertar tickets liberados (status = 'released')
INSERT INTO tickets (event_id, status, order_id, reserved_by, version) VALUES
  (1, 'released', 'ORD-2026-005', 'usuario5@example.com', 0),
  (2, 'released', 'ORD-2026-006', 'usuario6@example.com', 0);

-- 6. Insertar pagos en estado pending
INSERT INTO payments (ticket_id, status, provider_ref, amount_cents, currency) VALUES
  (12, 'pending', NULL, 5000, 'USD'),
  (13, 'pending', NULL, 7500, 'USD');

-- 7. Insertar pagos aprobados
INSERT INTO payments (ticket_id, status, provider_ref, amount_cents, currency) VALUES
  (14, 'approved', 'stripe_pi_123456789', 5000, 'USD'),
  (15, 'approved', 'stripe_pi_987654321', 7500, 'USD');

-- 8. Insertar pagos fallidos
INSERT INTO payments (ticket_id, status, provider_ref, amount_cents, currency) VALUES
  (11, 'failed', 'stripe_charge_failed_123', 5000, 'USD');

-- 9. Insertar histórico de cambios de tickets
INSERT INTO ticket_history (ticket_id, old_status, new_status, reason) VALUES
  (11, 'available', 'reserved', 'Usuario reservó el ticket'),
  (11, 'reserved', 'paid', 'Pago aprobado'),
  (11, 'paid', 'released', 'Usuario canceló después del pago'),
  (12, 'available', 'reserved', 'Usuario reservó el ticket'),
  (13, 'available', 'reserved', 'Usuario reservó el ticket'),
  (14, 'available', 'reserved', 'Usuario reservó el ticket'),
  (14, 'reserved', 'paid', 'Pago aprobado');

-- Verificar los datos insertados
SELECT '=== EVENTOS ===' as info;
SELECT * FROM events;

SELECT '=== TICKETS (Resumen) ===' as info;
SELECT 
  e.name as event_name,
  COUNT(*) as total_tickets,
  SUM(CASE WHEN t.status = 'available' THEN 1 ELSE 0 END) as available,
  SUM(CASE WHEN t.status = 'reserved' THEN 1 ELSE 0 END) as reserved,
  SUM(CASE WHEN t.status = 'paid' THEN 1 ELSE 0 END) as paid,
  SUM(CASE WHEN t.status = 'released' THEN 1 ELSE 0 END) as released
FROM tickets t
JOIN events e ON t.event_id = e.id
GROUP BY e.id, e.name;

SELECT '=== TICKETS DISPONIBLES ===' as info;
SELECT t.id, e.name, t.status, t.order_id, t.reserved_by 
FROM tickets t 
JOIN events e ON t.event_id = e.id 
WHERE t.status = 'available' 
LIMIT 5;

SELECT '=== TICKETS RESERVADOS ===' as info;
SELECT t.id, e.name, t.status, t.reserved_at, t.expires_at, t.reserved_by
FROM tickets t
JOIN events e ON t.event_id = e.id
WHERE t.status = 'reserved';

SELECT '=== PAGOS PENDING ===' as info;
SELECT p.id, t.id as ticket_id, e.name as event_name, p.amount_cents, p.currency, p.status
FROM payments p
JOIN tickets t ON p.ticket_id = t.id
JOIN events e ON t.event_id = e.id
WHERE p.status = 'pending';
