#!/bin/bash
set -e

# Script para configurar exchanges, colas y bindings en RabbitMQ usando la API HTTP
# Este script se ejecuta en el contenedor rabbitmq

echo "Esperando a que RabbitMQ esté completamente listo..."
for i in {1..30}; do
  if rabbitmq-diagnostics -q ping; then
    echo "✓ RabbitMQ listo"
    break
  fi
  echo "Intento $i/30: esperando RabbitMQ..."
  sleep 2
done

sleep 2  # Esperar un poco más para que la API HTTP esté lista

RABBIT_URL="http://localhost:15672/api"
RABBIT_USER="guest"
RABBIT_PASS="guest"
VHOST="%2F"  # "/" URL encoded

echo "Configurando exchanges y colas via HTTP API..."

# Crear exchange 'tickets' de tipo topic
echo "1. Creando exchange: tickets"
curl -i -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"type":"topic","durable":true,"auto_delete":false}' \
  "$RABBIT_URL/exchanges/$VHOST/tickets" 2>/dev/null || true

# Crear colas
echo "2. Creando cola: q.ticket.reserved"
curl -i -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true,"auto_delete":false}' \
  "$RABBIT_URL/queues/$VHOST/q.ticket.reserved" 2>/dev/null || true

echo "3. Creando cola: q.ticket.payments.approved"
curl -i -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true,"auto_delete":false}' \
  "$RABBIT_URL/queues/$VHOST/q.ticket.payments.approved" 2>/dev/null || true

echo "4. Creando cola: q.ticket.payments.rejected"
curl -i -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true,"auto_delete":false}' \
  "$RABBIT_URL/queues/$VHOST/q.ticket.payments.rejected" 2>/dev/null || true

echo "5. Creando cola: q.ticket.expired"
curl -i -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true,"auto_delete":false}' \
  "$RABBIT_URL/queues/$VHOST/q.ticket.expired" 2>/dev/null || true

echo "6. Creando cola delay: q.ticket.reserved.delay (TTL 5 min)"
curl -i -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true,"auto_delete":false,"arguments":{"x-message-ttl":300000,"x-dead-letter-exchange":"tickets","x-dead-letter-routing-key":"ticket.expired"}}' \
  "$RABBIT_URL/queues/$VHOST/q.ticket.reserved.delay" 2>/dev/null || true

# Crear bindings
echo "7. Bindeando: q.ticket.reserved -> ticket.reserved"
curl -i -u "$RABBIT_USER:$RABBIT_PASS" -X POST \
  -H "content-type:application/json" \
  -d '{"routing_key":"ticket.reserved"}' \
  "$RABBIT_URL/bindings/$VHOST/e/tickets/q/q.ticket.reserved" 2>/dev/null || true

echo "8. Bindeando: q.ticket.payments.approved -> ticket.payments.approved"
curl -i -u "$RABBIT_USER:$RABBIT_PASS" -X POST \
  -H "content-type:application/json" \
  -d '{"routing_key":"ticket.payments.approved"}' \
  "$RABBIT_URL/bindings/$VHOST/e/tickets/q/q.ticket.payments.approved" 2>/dev/null || true

echo "9. Bindeando: q.ticket.payments.rejected -> ticket.payments.rejected"
curl -i -u "$RABBIT_USER:$RABBIT_PASS" -X POST \
  -H "content-type:application/json" \
  -d '{"routing_key":"ticket.payments.rejected"}' \
  "$RABBIT_URL/bindings/$VHOST/e/tickets/q/q.ticket.payments.rejected" 2>/dev/null || true

echo "10. Bindeando: q.ticket.expired -> ticket.expired"
curl -i -u "$RABBIT_USER:$RABBIT_PASS" -X POST \
  -H "content-type:application/json" \
  -d '{"routing_key":"ticket.expired"}' \
  "$RABBIT_URL/bindings/$VHOST/e/tickets/q/q.ticket.expired" 2>/dev/null || true

echo ""
echo "========================================"
echo "✓ RabbitMQ configurado exitosamente!"
echo "========================================"
echo ""
echo "Exchanges:"
rabbitmqctl list_exchanges --no-table-headers name type durable

echo ""
echo "Queues:"
rabbitmqctl list_queues --no-table-headers name durable

echo ""
echo "Bindings:"
rabbitmqctl list_bindings --no-table-headers
