#!/bin/bash
# deploy.sh — Manual deployment script for staging
# Usage: ./deploy.sh [tag]
# Example: ./deploy.sh latest
#          ./deploy.sh abc1234
set -euo pipefail

TAG="${1:-latest}"
REGISTRY="ghcr.io"
REPO="wbincode/workbase"
COMPOSE_FILE="docker-compose.staging.yml"

echo "=== WorkBase Staging Deploy ==="
echo "Tag: ${TAG}"
echo "Time: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
echo ""

# Pull latest images
echo "→ Pulling images..."
docker pull "${REGISTRY}/${REPO}/workbase-api:${TAG}"
docker pull "${REGISTRY}/${REPO}/workbase-frontend:${TAG}"

# Tag as current
docker tag "${REGISTRY}/${REPO}/workbase-api:${TAG}" workbase-api:current
docker tag "${REGISTRY}/${REPO}/workbase-frontend:${TAG}" workbase-frontend:current

# Run migrations before deploy
echo "→ Running database migrations..."
docker compose -f "${COMPOSE_FILE}" run --rm workbase-api \
  dotnet WorkBase.Host.dll --migrate-only || echo "WARN: Migration step skipped"

# Deploy
echo "→ Deploying services..."
docker compose -f "${COMPOSE_FILE}" up -d --remove-orphans

# Wait for health check
echo "→ Waiting for health check..."
sleep 10

for i in $(seq 1 6); do
  if curl -sf http://localhost:5000/health > /dev/null 2>&1; then
    echo "✅ Health check passed"
    break
  fi
  if [ "$i" -eq 6 ]; then
    echo "❌ Health check failed after 30s"
    docker compose -f "${COMPOSE_FILE}" logs workbase-api --tail=50
    exit 1
  fi
  sleep 5
done

# Cleanup
echo "→ Cleaning up old images..."
docker image prune -f

echo ""
echo "=== Deploy complete ==="
echo "API: http://localhost:5000"
echo "Frontend: http://localhost:80"
