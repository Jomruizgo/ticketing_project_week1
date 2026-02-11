#!/bin/sh
set -e

if [ -z "${POSTGRES_HOST}" ] || [ -z "${POSTGRES_USER}" ] || [ -z "${POSTGRES_DB}" ]; then
  echo "Missing required env vars: POSTGRES_HOST, POSTGRES_USER, POSTGRES_DB" >&2
  exit 1
fi

export PGPASSWORD="${POSTGRES_PASSWORD}"

echo "$(date -u +%Y-%m-%dT%H:%M:%SZ) running expiration job"

/usr/bin/psql \
  --host "${POSTGRES_HOST}" \
  --port "${POSTGRES_PORT:-5432}" \
  --username "${POSTGRES_USER}" \
  --dbname "${POSTGRES_DB}" \
  --command "\
    WITH updated AS (\
      UPDATE tickets\
      SET status = 'available',\
          reserved_at = NULL,\
          expires_at = NULL,\
          order_id = NULL,\
          reserved_by = NULL,\
          version = version + 1\
      WHERE status = 'reserved'\
        AND paid_at IS NULL\
        AND expires_at IS NOT NULL\
        AND expires_at <= NOW()\
      RETURNING 1\
    )\
    SELECT COUNT(*) AS updated_count FROM updated;\
  "
