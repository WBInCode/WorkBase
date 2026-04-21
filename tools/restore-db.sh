#!/bin/bash
# restore-db.sh — Restore a WorkBase PostgreSQL backup
# Usage: ./restore-db.sh <backup-file.sql.gz>
# Example: ./restore-db.sh /opt/workbase/backups/workbase_20250101_020000.sql.gz
set -euo pipefail

if [ -z "${1:-}" ]; then
  echo "Usage: $0 <backup-file.sql.gz>"
  echo "Available backups:"
  ls -lt "${BACKUP_DIR:-/opt/workbase/backups}"/*.sql.gz 2>/dev/null | head -10
  exit 1
fi

BACKUP_FILE="$1"
DB_NAME="${DATABASE_NAME:-workbase}"
DB_USER="${DATABASE_USER:-workbase}"
CONTAINER_NAME="${PG_CONTAINER:-workbase-postgres}"

if [ ! -f "${BACKUP_FILE}" ]; then
  echo "❌ Backup file not found: ${BACKUP_FILE}"
  exit 1
fi

echo "=== WorkBase Database Restore ==="
echo "File: ${BACKUP_FILE}"
echo "Database: ${DB_NAME}"
echo ""
read -p "⚠️  This will DROP and recreate '${DB_NAME}'. Continue? (yes/no) " CONFIRM
if [ "${CONFIRM}" != "yes" ]; then
  echo "Restore cancelled."
  exit 0
fi

echo "→ Restoring..."

if docker ps --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
  # Stop API to release connections
  docker stop workbase-api 2>/dev/null || true

  docker exec "${CONTAINER_NAME}" dropdb -U "${DB_USER}" --if-exists "${DB_NAME}"
  docker exec "${CONTAINER_NAME}" createdb -U "${DB_USER}" "${DB_NAME}"
  gunzip -c "${BACKUP_FILE}" | docker exec -i "${CONTAINER_NAME}" psql -U "${DB_USER}" -d "${DB_NAME}" -q

  # Restart API
  docker start workbase-api 2>/dev/null || true
else
  PGPASSWORD="${DATABASE_PASSWORD:-}" dropdb -h localhost -U "${DB_USER}" --if-exists "${DB_NAME}"
  PGPASSWORD="${DATABASE_PASSWORD:-}" createdb -h localhost -U "${DB_USER}" "${DB_NAME}"
  gunzip -c "${BACKUP_FILE}" | PGPASSWORD="${DATABASE_PASSWORD:-}" psql -h localhost -U "${DB_USER}" -d "${DB_NAME}" -q
fi

echo "✅ Restore complete from: $(basename ${BACKUP_FILE})"
