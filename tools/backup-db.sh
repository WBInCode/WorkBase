#!/bin/bash
# backup-db.sh — PostgreSQL backup script for WorkBase
# Creates compressed pg_dump backups with retention policy.
# Usage: ./backup-db.sh
# Set environment variables or defaults will be used.
# Intended to run as a cron job (see setup-staging.sh).
set -euo pipefail

# --- Configuration ---
BACKUP_DIR="${BACKUP_DIR:-/opt/workbase/backups}"
RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-30}"
DB_HOST="${DATABASE_HOST:-localhost}"
DB_PORT="${DATABASE_PORT:-5432}"
DB_NAME="${DATABASE_NAME:-workbase}"
DB_USER="${DATABASE_USER:-workbase}"
TIMESTAMP="$(date -u +%Y%m%d_%H%M%S)"
BACKUP_FILE="${BACKUP_DIR}/${DB_NAME}_${TIMESTAMP}.sql.gz"
CONTAINER_NAME="${PG_CONTAINER:-workbase-postgres}"

echo "[$(date -u +%Y-%m-%dT%H:%M:%SZ)] Starting backup of '${DB_NAME}'..."

# Ensure backup directory exists
mkdir -p "${BACKUP_DIR}"

# --- Backup ---
# Try container-based backup first (staging/production), fall back to direct pg_dump
if docker ps --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
  echo "→ Using container: ${CONTAINER_NAME}"
  docker exec "${CONTAINER_NAME}" \
    pg_dump -U "${DB_USER}" -d "${DB_NAME}" --no-owner --no-acl \
    | gzip > "${BACKUP_FILE}"
else
  echo "→ Using local pg_dump"
  PGPASSWORD="${DATABASE_PASSWORD:-}" pg_dump \
    -h "${DB_HOST}" \
    -p "${DB_PORT}" \
    -U "${DB_USER}" \
    -d "${DB_NAME}" \
    --no-owner --no-acl \
    | gzip > "${BACKUP_FILE}"
fi

# Verify backup
BACKUP_SIZE=$(stat -c%s "${BACKUP_FILE}" 2>/dev/null || stat -f%z "${BACKUP_FILE}" 2>/dev/null)
if [ "${BACKUP_SIZE}" -lt 100 ]; then
  echo "❌ Backup file is suspiciously small (${BACKUP_SIZE} bytes). Aborting."
  rm -f "${BACKUP_FILE}"
  exit 1
fi

echo "✅ Backup created: ${BACKUP_FILE} ($(numfmt --to=iec ${BACKUP_SIZE}))"

# --- Retention: remove old backups ---
echo "→ Removing backups older than ${RETENTION_DAYS} days..."
DELETED=$(find "${BACKUP_DIR}" -name "${DB_NAME}_*.sql.gz" -mtime +${RETENTION_DAYS} -print -delete | wc -l)
echo "   Removed ${DELETED} old backup(s)"

# --- Optional: Upload to S3/MinIO ---
if [ -n "${S3_BUCKET:-}" ] && command -v mc &> /dev/null; then
  echo "→ Uploading to MinIO/S3: ${S3_BUCKET}..."
  mc cp "${BACKUP_FILE}" "${S3_BUCKET}/backups/$(basename ${BACKUP_FILE})"
  echo "✅ Uploaded to ${S3_BUCKET}"
elif [ -n "${S3_BUCKET:-}" ] && command -v aws &> /dev/null; then
  echo "→ Uploading to S3: ${S3_BUCKET}..."
  aws s3 cp "${BACKUP_FILE}" "s3://${S3_BUCKET}/backups/$(basename ${BACKUP_FILE})"
  echo "✅ Uploaded to s3://${S3_BUCKET}"
fi

echo "[$(date -u +%Y-%m-%dT%H:%M:%SZ)] Backup complete."
