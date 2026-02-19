#!/usr/bin/env bash
set -euo pipefail

CRUD_BASE_URL="${CRUD_BASE_URL:-http://localhost:8002}"
PRODUCER_BASE_URL="${PRODUCER_BASE_URL:-http://localhost:8001}"
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-60}"
POLL_INTERVAL_SECONDS="${POLL_INTERVAL_SECONDS:-2}"

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "[ERROR] Command not found: $1"
    exit 1
  fi
}

json_get() {
  local key="$1"
  python3 -c "import json,sys; print(json.load(sys.stdin)$key)"
}

http_post() {
  local url="$1"
  local body="$2"
  curl -sS -X POST "$url" -H "Content-Type: application/json" -d "$body"
}

http_get_status() {
  local url="$1"
  curl -sS -o /dev/null -w "%{http_code}" "$url"
}

find_available_ticket() {
  local events_json event_ids event_id_candidate tickets_json ticket_id_candidate

  events_json="$(curl -sS "$CRUD_BASE_URL/api/events")"
  event_ids="$(echo "$events_json" | python3 -c "import json,sys; data=json.load(sys.stdin); print(' '.join(str(e.get('id')) for e in data if isinstance(e, dict) and e.get('id') is not None))" 2>/dev/null || true)"

  for event_id_candidate in $event_ids; do
    tickets_json="$(curl -sS "$CRUD_BASE_URL/api/tickets/event/$event_id_candidate")"
    ticket_id_candidate="$(echo "$tickets_json" | python3 -c "import json,sys; data=json.load(sys.stdin); avail=[t for t in data if isinstance(t, dict) and t.get('status')=='available']; print(avail[0]['id'] if avail else '')" 2>/dev/null || true)"

    if [[ -n "$ticket_id_candidate" ]]; then
      echo "$event_id_candidate,$ticket_id_candidate"
      return 0
    fi
  done

  return 1
}

require_command curl
require_command python3

echo "[1/8] Checking service health endpoints..."
producer_health="$(http_get_status "$PRODUCER_BASE_URL/health")"
crud_health="$(http_get_status "$CRUD_BASE_URL/health")"

if [[ "$producer_health" != "200" ]]; then
  echo "[ERROR] Producer health failed: HTTP $producer_health"
  exit 1
fi
if [[ "$crud_health" != "200" ]]; then
  echo "[ERROR] CRUD health failed: HTTP $crud_health"
  exit 1
fi

echo "[2/8] Creating event in CRUD..."
starts_at="$(date -u -d '+2 hour' +%Y-%m-%dT%H:%M:%SZ 2>/dev/null || date -u +%Y-%m-%dT%H:%M:%SZ)"
event_payload="{\"name\":\"E2E Event $(date +%s)\",\"startsAt\":\"$starts_at\"}"
event_response="$(http_post "$CRUD_BASE_URL/api/events" "$event_payload")"
event_id="$(echo "$event_response" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('id','') if isinstance(d,dict) else '')" 2>/dev/null || true)"

if [[ -z "$event_id" ]]; then
  echo "[WARN] Could not create event automatically. Falling back to existing available ticket..."
fi

echo "[3/8] Creating ticket batch in CRUD..."
ticket_id=""

if [[ -n "$event_id" ]]; then
  tickets_payload="{\"eventId\":$event_id,\"quantity\":1}"
  tickets_response="$(http_post "$CRUD_BASE_URL/api/tickets/bulk" "$tickets_payload")"
  ticket_id="$(echo "$tickets_response" | python3 -c "import json,sys; d=json.load(sys.stdin); print((d[0]['id'] if isinstance(d,list) and d else d.get('id','')) if isinstance(d,(list,dict)) else '')" 2>/dev/null || true)"
fi

if [[ -z "$ticket_id" ]]; then
  echo "[WARN] Ticket creation failed or returned unexpected payload. Searching for existing available ticket..."
  if [[ -n "${tickets_response:-}" ]]; then
    echo "[WARN] /api/tickets/bulk response: $tickets_response"
  fi
  fallback_pair="$(find_available_ticket || true)"
  if [[ -z "$fallback_pair" ]]; then
    echo "[ERROR] No available ticket found and ticket creation failed."
    exit 1
  fi
  event_id="${fallback_pair%,*}"
  ticket_id="${fallback_pair#*,}"
fi

echo "[4/8] Opening SSE stream for reserve and sending reserve request..."
reserve_sse_file="$(mktemp)"
curl -sS -N --max-time "$TIMEOUT_SECONDS" "$CRUD_BASE_URL/api/tickets/$ticket_id/stream" > "$reserve_sse_file" &
reserve_sse_pid=$!
sleep 0.2

reserve_payload="{\"eventId\":$event_id,\"ticketId\":$ticket_id,\"orderId\":\"ORD-E2E-$(date +%s)\",\"reservedBy\":\"e2e@test.com\",\"expiresInSeconds\":300}"
reserve_response="$(http_post "$PRODUCER_BASE_URL/api/tickets/reserve" "$reserve_payload")"
reserve_ticket_id="$(echo "$reserve_response" | json_get "['ticketId']")"

if [[ "$reserve_ticket_id" != "$ticket_id" ]]; then
  echo "[ERROR] Reserve response ticketId mismatch. expected=$ticket_id got=$reserve_ticket_id"
  exit 1
fi

wait "$reserve_sse_pid" || true
reserve_data_line="$(grep '^data:' "$reserve_sse_file" | head -n 1 | sed 's/^data: //')"
reserve_status="$(echo "$reserve_data_line" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('status',''))" 2>/dev/null || true)"
rm -f "$reserve_sse_file"

if [[ "$reserve_status" != "reserved" ]]; then
  echo "[ERROR] Reserve SSE status mismatch. expected=reserved got=${reserve_status:-<empty>}"
  exit 1
fi

echo "[5/8] Opening SSE stream for payment and sending payment request..."
payment_sse_file="$(mktemp)"
curl -sS -N --max-time "$TIMEOUT_SECONDS" "$CRUD_BASE_URL/api/tickets/$ticket_id/stream" > "$payment_sse_file" &
payment_sse_pid=$!
sleep 0.2

payment_payload="{\"ticketId\":$ticket_id,\"eventId\":$event_id,\"amountCents\":5000,\"currency\":\"USD\",\"paymentBy\":\"e2e@test.com\",\"paymentMethodId\":\"card_e2e\"}"
payment_response="$(http_post "$PRODUCER_BASE_URL/api/payments/process" "$payment_payload")"
payment_ticket_id="$(echo "$payment_response" | json_get "['ticketId']")"

if [[ "$payment_ticket_id" != "$ticket_id" ]]; then
  echo "[ERROR] Payment response ticketId mismatch. expected=$ticket_id got=$payment_ticket_id"
  exit 1
fi

echo "[6/8] Waiting payment status update via SSE..."
wait "$payment_sse_pid" || true
data_line="$(grep '^data:' "$payment_sse_file" | head -n 1 | sed 's/^data: //')"
rm -f "$payment_sse_file"

if [[ -z "$data_line" ]]; then
  echo "[ERROR] SSE timeout or empty stream. timeout=${TIMEOUT_SECONDS}s"
  echo "[ERROR] SSE raw output: <empty>"
  exit 1
fi

final_status="$(echo "$data_line" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('status',''))" 2>/dev/null || true)"

if [[ -z "$final_status" ]]; then
  echo "[ERROR] Could not parse SSE payload status. payload=$data_line"
  exit 1
fi

echo "[7/8] Final validation..."
if [[ "$final_status" != "paid" && "$final_status" != "released" ]]; then
  echo "[ERROR] Unexpected final status: $final_status"
  exit 1
fi

echo "[8/8] E2E SUCCESS"
echo "event_id=$event_id"
echo "ticket_id=$ticket_id"
echo "final_status=$final_status"
