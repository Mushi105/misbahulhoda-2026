#!/bin/bash
# Run this ONCE on your Contabo server to set it up for the first time.
# After this, all future deploys happen automatically via GitHub Actions.
#
# Usage:
#   ssh root@YOUR_SERVER_IP
#   bash <(curl -s https://raw.githubusercontent.com/mubashirhasan/misbahulhoda-2026/main/scripts/server-setup.sh)
#   OR just copy-paste this file and run: bash server-setup.sh

set -e

DOMAIN="misbahulhoda.mubashirhasan.dev"
APP_DIR="/opt/misbahulhoda"
GITHUB_REPO="https://github.com/mubashirhasan/misbahulhoda-2026.git"

echo "=== Misbah ul Hoda — Server Setup ==="

# 1. Update system
echo "[1/7] Updating system..."
apt-get update -qq && apt-get upgrade -y -qq

# 2. Install Docker
echo "[2/7] Installing Docker..."
if ! command -v docker &> /dev/null; then
  curl -fsSL https://get.docker.com | sh
  systemctl enable docker
  systemctl start docker
else
  echo "  Docker already installed."
fi

# 3. Install Nginx + Certbot
echo "[3/7] Installing Nginx + Certbot..."
apt-get install -y nginx certbot python3-certbot-nginx git

# 4. Clone repo
echo "[4/7] Cloning repository..."
if [ -d "$APP_DIR" ]; then
  echo "  Directory exists, pulling latest..."
  cd "$APP_DIR" && git pull origin main
else
  git clone "$GITHUB_REPO" "$APP_DIR"
fi
cd "$APP_DIR"

# 5. Start app
echo "[5/7] Starting app with Docker Compose..."
docker compose up -d --build

# 6. Configure Nginx
echo "[6/7] Configuring Nginx..."
cp "$APP_DIR/scripts/nginx-server.conf" /etc/nginx/sites-available/misbahulhoda
ln -sf /etc/nginx/sites-available/misbahulhoda /etc/nginx/sites-enabled/misbahulhoda
rm -f /etc/nginx/sites-enabled/default
nginx -t && systemctl reload nginx

# 7. SSL with Let's Encrypt
echo "[7/7] Setting up SSL (HTTPS)..."
certbot --nginx -d "$DOMAIN" --non-interactive --agree-tos -m mubashir105@gmail.com

echo ""
echo "========================================="
echo "  SETUP COMPLETE!"
echo "========================================="
echo ""
echo "  App:     https://$DOMAIN"
echo "  Admin:   https://$DOMAIN/admin"
echo "  API:     https://$DOMAIN/api/swagger"
echo ""
echo "  Future deploys: just push to GitHub main branch."
echo "  GitHub Actions will auto-deploy within 1-2 minutes."
echo ""
