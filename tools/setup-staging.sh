#!/bin/bash
# setup-staging.sh — One-time staging server setup
# Run on a fresh Ubuntu 22.04+ VPS
# Usage: curl -fsSL https://raw.githubusercontent.com/.../setup-staging.sh | sudo bash
set -euo pipefail

echo "=== WorkBase Staging Server Setup ==="

# --- System packages ---
echo "→ Installing prerequisites..."
apt-get update -qq
apt-get install -y -qq curl git ufw fail2ban unattended-upgrades

# --- Docker ---
if ! command -v docker &> /dev/null; then
  echo "→ Installing Docker..."
  curl -fsSL https://get.docker.com | sh
  systemctl enable docker
  systemctl start docker
fi

# --- Docker Compose (plugin) ---
if ! docker compose version &> /dev/null; then
  echo "→ Installing Docker Compose plugin..."
  apt-get install -y -qq docker-compose-plugin
fi

# --- Firewall ---
echo "→ Configuring firewall..."
ufw default deny incoming
ufw default allow outgoing
ufw allow ssh
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable

# --- Application directory ---
echo "→ Setting up /opt/workbase..."
mkdir -p /opt/workbase/backups
mkdir -p /opt/workbase/docker/postgres
mkdir -p /opt/workbase/docker/keycloak

# --- Deploy user ---
if ! id "deploy" &>/dev/null; then
  echo "→ Creating deploy user..."
  useradd -m -s /bin/bash -G docker deploy
  mkdir -p /home/deploy/.ssh
  chmod 700 /home/deploy/.ssh
  echo "# Add your SSH public key here" > /home/deploy/.ssh/authorized_keys
  chmod 600 /home/deploy/.ssh/authorized_keys
  chown -R deploy:deploy /home/deploy/.ssh
fi

chown -R deploy:deploy /opt/workbase

# --- GHCR login helper ---
echo "→ Creating GHCR login helper..."
cat > /opt/workbase/ghcr-login.sh << 'SCRIPT'
#!/bin/bash
# Usage: ./ghcr-login.sh <github-username> <personal-access-token>
echo "$2" | docker login ghcr.io -u "$1" --password-stdin
SCRIPT
chmod +x /opt/workbase/ghcr-login.sh

# --- Backup cron ---
echo "→ Setting up daily backup cron..."
cat > /etc/cron.d/workbase-backup << 'CRON'
# WorkBase daily database backup at 02:00 UTC
0 2 * * * deploy /opt/workbase/backup-db.sh >> /opt/workbase/backups/backup.log 2>&1
CRON

echo ""
echo "=== Setup Complete ==="
echo ""
echo "Next steps:"
echo "  1. Copy .env from staging.env.example to /opt/workbase/.env"
echo "  2. Edit /opt/workbase/.env with real credentials"
echo "  3. Copy docker-compose.staging.yml to /opt/workbase/"
echo "  4. Copy docker/ config files (postgres/, keycloak/)"
echo "  5. Login to GHCR: /opt/workbase/ghcr-login.sh <user> <token>"
echo "  6. Deploy: cd /opt/workbase && docker compose -f docker-compose.staging.yml up -d"
echo "  7. Add deploy user SSH key to /home/deploy/.ssh/authorized_keys"
