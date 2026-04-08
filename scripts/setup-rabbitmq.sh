#!/bin/bash
# Script manual para crear exchanges, colas y bindings en RabbitMQ via API HTTP
# Ejecutar después de: docker compose up -d

# Variables de entorno o defaults
RABBIT_HOST="${RABBITMQ_HOST:-localhost}"
RABBIT_PORT="${RABBITMQ_PORT:-15672}"
RABBIT_USER="${RABBITMQ_DEFAULT_USER:-guest}"
RABBIT_PASS="${RABBITMQ_DEFAULT_PASS:-guest}"
VHOST="%2F"  # "/" URL encoded

RABBIT_URL="http://$RABBIT_HOST:$RABBIT_PORT/api"

echo "Configurando RabbitMQ en http://$RABBIT_HOST:$RABBIT_PORT..."
echo ""

# Exchange
echo "[1/10] Creando exchange: tickets"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"type":"topic","durable":true}' \
  "$RABBIT_URL/exchanges/$VHOST/tickets" && echo " ✓" || echo " ✗"

# Queues
echo "[2/10] Creando queue: q.ticket.reserved"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true}' \
  "$RABBIT_URL/queues/$VHOST/q.ticket.reserved" && echo " ✓" || echo " ✗"

echo "[3/10] Creando queue: q.ticket.payments.approved"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true}' \
  "$RABBIT_URL/queues/$VHOST/q.ticket.payments.approved" && echo " ✓" || echo " ✗"

echo "[4/10] Creando queue: q.ticket.payments.rejected"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true}' \
  "$RABBIT_URL/queues/$VHOST/q.ticket.payments.rejected" && echo " ✓" || echo " ✗"

echo "[5/12] Creando queue: q.ticket.payment.requested"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true}' \
  "$RABBIT_URL/queues/$VHOST/q.ticket.payment.requested" && echo " ✓" || echo " ✗"

echo "[6/12] Creando queue: q.ticket.status.changed"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true}' \
  "$RABBIT_URL/queues/$VHOST/q.ticket.status.changed" && echo " ✓" || echo " ✗"

echo "[7/12] Creando queue: q.ticket.expired"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true}' \
  "$RABBIT_URL/queues/$VHOST/q.ticket.expired" && echo " ✓" || echo " ✗"

echo "[8/12] Creando queue delay: q.ticket.reserved.delay (TTL 5 min)"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true,"arguments":{"x-message-ttl":300000,"x-dead-letter-exchange":"tickets","x-dead-letter-routing-key":"ticket.expired"}}' \
  "$RABBIT_URL/queues/$VHOST/q.ticket.reserved.delay" && echo " ✓" || echo " ✗"

# Bindings
echo "[9/12] Bindeando: q.ticket.reserved ← ticket.reserved"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X POST \
  -H "content-type:application/json" \
  -d '{"routing_key":"ticket.reserved"}' \
  "$RABBIT_URL/bindings/$VHOST/e/tickets/q/q.ticket.reserved" && echo " ✓" || echo " ✗"

echo "[10/12] Bindeando: q.ticket.payments.approved ← ticket.payments.approved"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X POST \
  -H "content-type:application/json" \
  -d '{"routing_key":"ticket.payments.approved"}' \
  "$RABBIT_URL/bindings/$VHOST/e/tickets/q/q.ticket.payments.approved" && echo " ✓" || echo " ✗"

echo "[11/12] Bindeando: q.ticket.payments.rejected ← ticket.payments.rejected"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X POST \
  -H "content-type:application/json" \
  -d '{"routing_key":"ticket.payments.rejected"}' \
  "$RABBIT_URL/bindings/$VHOST/e/tickets/q/q.ticket.payments.rejected" && echo " ✓" || echo " ✗"

echo "[12/14] Bindeando: q.ticket.payment.requested ← ticket.payment.requested"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X POST \
  -H "content-type:application/json" \
  -d '{"routing_key":"ticket.payment.requested"}' \
  "$RABBIT_URL/bindings/$VHOST/e/tickets/q/q.ticket.payment.requested" && echo " ✓" || echo " ✗"

echo "[13/14] Bindeando: q.ticket.status.changed ← ticket.status.changed"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X POST \
  -H "content-type:application/json" \
  -d '{"routing_key":"ticket.status.changed"}' \
  "$RABBIT_URL/bindings/$VHOST/e/tickets/q/q.ticket.status.changed" && echo " ✓" || echo " ✗"

echo "[14/14] Bindeando: q.ticket.expired ← ticket.expired"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X POST \
  -H "content-type:application/json" \
  -d '{"routing_key":"ticket.expired"}' \
  "$RABBIT_URL/bindings/$VHOST/e/tickets/q/q.ticket.expired" && echo " ✓" || echo " ✗"

# --- Waitlist opportunity assignment (HU3) ---

echo "[15/22] Creando queue: q.ticket.released"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true}' \
  "$RABBIT_URL/queues/$VHOST/q.ticket.released" && echo " ✓" || echo " ✗"

echo "[16/22] Creando queue: q.ticket.returned"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true}' \
  "$RABBIT_URL/queues/$VHOST/q.ticket.returned" && echo " ✓" || echo " ✗"

WAITLIST_TTL="${WAITLIST_OPPORTUNITY_TTL_MS:-900000}"

echo "[17/22] Creando queue delay: q.waitlist.opportunity.delay (TTL ${WAITLIST_TTL}ms)"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d "{\"durable\":true,\"arguments\":{\"x-message-ttl\":${WAITLIST_TTL},\"x-dead-letter-exchange\":\"tickets\",\"x-dead-letter-routing-key\":\"waitlist.opportunity.expired\"}}" \
  "$RABBIT_URL/queues/$VHOST/q.waitlist.opportunity.delay" && echo " ✓" || echo " ✗"

echo "[18/22] Creando queue: q.waitlist.opportunity.expired"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true}' \
  "$RABBIT_URL/queues/$VHOST/q.waitlist.opportunity.expired" && echo " ✓" || echo " ✗"

echo "[19/22] Bindeando: q.ticket.released ← ticket.released"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X POST \
  -H "content-type:application/json" \
  -d '{"routing_key":"ticket.released"}' \
  "$RABBIT_URL/bindings/$VHOST/e/tickets/q/q.ticket.released" && echo " ✓" || echo " ✗"

echo "[20/22] Bindeando: q.ticket.returned ← ticket.returned_to_inventory"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X POST \
  -H "content-type:application/json" \
  -d '{"routing_key":"ticket.returned_to_inventory"}' \
  "$RABBIT_URL/bindings/$VHOST/e/tickets/q/q.ticket.returned" && echo " ✓" || echo " ✗"

echo "[21/22] Bindeando: q.waitlist.opportunity.expired ← waitlist.opportunity.expired"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X POST \
  -H "content-type:application/json" \
  -d '{"routing_key":"waitlist.opportunity.expired"}' \
  "$RABBIT_URL/bindings/$VHOST/e/tickets/q/q.waitlist.opportunity.expired" && echo " ✓" || echo " ✗"

echo ""
echo "✓ Configuración completada!"
echo ""
echo "Verificando resultado:"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" "$RABBIT_URL/queues/$VHOST" | jq '.[] | {name: .name, durable: .durable, messages: .messages}'
