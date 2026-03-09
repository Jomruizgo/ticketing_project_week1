#!/usr/bin/env bash
set -euo pipefail

CRUD_BASE_URL="${CRUD_BASE_URL:-http://localhost:8002}"
PRODUCER_BASE_URL="${PRODUCER_BASE_URL:-http://localhost:8001}"
RABBIT_HTTP_URL="${RABBIT_HTTP_URL:-http://localhost:15672}"
RABBIT_USER="${RABBIT_USER:-guest}"
RABBIT_PASS="${RABBIT_PASS:-guest}"

HEALTH_TIMEOUT_SECONDS="${HEALTH_TIMEOUT_SECONDS:-90}"
POLL_INTERVAL_SECONDS="${POLL_INTERVAL_SECONDS:-1}"
STATE_TIMEOUT_SECONDS="${STATE_TIMEOUT_SECONDS:-45}"

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "[ERROR] Command not found: $1"
    exit 1
  fi
}

find_available_ticket() {
  local events_json event_ids event_id_candidate tickets_json ticket_id_candidate

  events_json="$(curl -sS "$CRUD_BASE_URL/api/events")"
  event_ids="$(echo "$events_json" | python3 -c "import json,sys; data=json.load(sys.stdin); print(' '.join(str(e.get('id')) for e in data if isinstance(e, dict) and e.get('id') is not None))" 2>/dev/null || true)"

  for event_id_candidate in $event_ids; do
    tickets_json="$(curl -sS "$CRUD_BASE_URL/api/tickets/event/$event_id_candidate")"
    ticket_id_candidate="$(echo "$tickets_json" | python3 -c "import json,sys; data=json.load(sys.stdin); avail=[t for t in data if isinstance(t, dict) and str(t.get('status','')).lower()=='available']; print(avail[0]['id'] if avail else '')" 2>/dev/null || true)"

    if [[ -n "$ticket_id_candidate" ]]; then
      echo "$event_id_candidate,$ticket_id_candidate"
      return 0
    fi
  done

  return 1
}

wait_http_200() {
  local url="$1"
  local timeout="$2"
  local start now elapsed code

  start="$(date +%s)"
  while true; do
    code="$(curl -sS -o /dev/null -w "%{http_code}" "$url" || true)"
    if [[ "$code" == "200" ]]; then
      return 0
    fi

    now="$(date +%s)"
    elapsed="$((now - start))"
    if (( elapsed >= timeout )); then
      echo "[ERROR] Timeout waiting HTTP 200: $url (last=$code)"
      return 1
    fi
    sleep "$POLL_INTERVAL_SECONDS"
  done
}

wait_ticket_status() {
  local ticket_id="$1"
  local expected_status="$2"
  local timeout="$3"
  local start now elapsed current

  start="$(date +%s)"
  while true; do
    current="$(curl -sS "$CRUD_BASE_URL/api/tickets/$ticket_id" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("status",""))' 2>/dev/null || true)"
    if [[ "${current,,}" == "${expected_status,,}" ]]; then
      echo "[OK] ticket=$ticket_id status=$current"
      return 0
    fi

    now="$(date +%s)"
    elapsed="$((now - start))"
    if (( elapsed >= timeout )); then
      echo "[ERROR] ticket=$ticket_id status timeout. expected=$expected_status last=${current:-<empty>}"
      return 1
    fi
    sleep "$POLL_INTERVAL_SECONDS"
  done
}

require_command curl
require_command python3

echo "[1/7] Checking service health..."
wait_http_200 "$CRUD_BASE_URL/health" "$HEALTH_TIMEOUT_SECONDS"
wait_http_200 "$PRODUCER_BASE_URL/health" "$HEALTH_TIMEOUT_SECONDS"

echo "[2/7] Preparing ticket to reserve..."
event_id=""
ticket_id=""

starts_at="$(date -u -d '+2 hour' +%Y-%m-%dT%H:%M:%SZ 2>/dev/null || date -u +%Y-%m-%dT%H:%M:%SZ)"
event_payload="{\"name\":\"DevA Expiration Verification $(date +%s)\",\"startsAt\":\"$starts_at\"}"

for attempt in 1 2 3; do
  event_json="$(curl -sS -X POST "$CRUD_BASE_URL/api/events" -H "Content-Type: application/json" -d "$event_payload" || true)"
  event_id="$(echo "$event_json" | python3 -c 'import json,sys; d=json.load(sys.stdin); print(d.get("id","") if isinstance(d,dict) else "")' 2>/dev/null || true)"
  if [[ -n "$event_id" ]]; then
    break
  fi
  sleep 1
done

if [[ -n "$event_id" ]]; then
  echo "[3/7] Creating one ticket for event=$event_id..."
  tickets_json="$(curl -sS -X POST "$CRUD_BASE_URL/api/tickets/bulk" -H "Content-Type: application/json" -d "{\"eventId\":$event_id,\"quantity\":1}" || true)"
  ticket_id="$(echo "$tickets_json" | python3 -c 'import json,sys; d=json.load(sys.stdin); print(d[0]["id"] if isinstance(d,list) and d else "")' 2>/dev/null || true)"
fi

if [[ -z "$ticket_id" ]]; then
  echo "[WARN] Could not create event/ticket in CRUD, trying fallback available ticket..."
  fallback_pair="$(find_available_ticket || true)"
  if [[ -z "$fallback_pair" ]]; then
    echo "[ERROR] No available ticket found for fallback"
    echo "event_json=${event_json:-<none>}"
    echo "tickets_json=${tickets_json:-<none>}"
    exit 1
  fi
  event_id="${fallback_pair%,*}"
  ticket_id="${fallback_pair#*,}"
fi

echo "[4/7] Reserving ticket=$ticket_id via producer..."
reserve_payload="{\"eventId\":$event_id,\"ticketId\":$ticket_id,\"orderId\":\"ORD-DEV-A-$(date +%s)\",\"reservedBy\":\"deva@test.com\",\"expiresInSeconds\":300}"
reserve_json="$(curl -sS -X POST "$PRODUCER_BASE_URL/api/tickets/reserve" -H "Content-Type: application/json" -d "$reserve_payload")"
echo "[INFO] reserve_response=$reserve_json"

wait_ticket_status "$ticket_id" "reserved" "$STATE_TIMEOUT_SECONDS"

echo "[5/7] Publishing ticket.expired for ticket=$ticket_id..."
publish_body="$(python3 - <<PY
import json
ticket_id = int("$ticket_id")
print(json.dumps({
  "properties": {},
  "routing_key": "ticket.expired",
  "payload": json.dumps({"ticketId": ticket_id}),
  "payload_encoding": "string"
}))
PY
)"
publish_response="$(curl -sS -u "$RABBIT_USER:$RABBIT_PASS" -H 'content-type:application/json' -X POST "$RABBIT_HTTP_URL/api/exchanges/%2F/tickets/publish" -d "$publish_body")"
echo "[INFO] publish_response=$publish_response"

routed="$(echo "$publish_response" | python3 -c 'import json,sys; d=json.load(sys.stdin); print(str(d.get("routed", False)).lower())' 2>/dev/null || echo false)"
if [[ "$routed" != "true" ]]; then
  echo "[ERROR] RabbitMQ publish was not routed"
  exit 1
fi

echo "[6/7] Waiting for release state..."
wait_ticket_status "$ticket_id" "released" "$STATE_TIMEOUT_SECONDS"

echo "[7/7] Verification SUCCESS"
echo "event_id=$event_id"
echo "ticket_id=$ticket_id"
echo "final_status=released"
