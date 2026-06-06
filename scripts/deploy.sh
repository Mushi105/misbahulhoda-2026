#!/bin/bash
# Misbah ul Hoda — Arbaeen 2026
# Deploy script — run this on your Contabo server
# Usage: bash deploy.sh

set -e

DOMAIN="misbahulhoda.mubashirhasan.dev"
APP_DIR="/opt/misbahulhoda"

echo "=== Misbah ul Hoda — Arbaeen 2026 Deploy ==="

# 1. Pull latest code
if [ -d "$APP_DIR" ]; then
  echo "[1/6] Pulling latest code..."
  cd "$APP_DIR"
  git pull origin main
else
  echo "[1/6] Cloning repo..."
  git clone https://github.com/YOUR_GITHUB_USERNAME/YOUR_REPO_NAME.git "$APP_DIR"
  cd "$APP_DIR"
fi

# 2. Stop old containers
echo "[2/6] Stopping old containers..."
docker compose down --remove-orphans || true

# 3. Build & start
echo "[3/6] Building and starting containers..."
docker compose up -d --build

# 4. Run database migrations
echo "[4/6] Running database migrations..."
sleep 10  # Wait for postgres to be ready
docker exec misbahuda_api dotnet ef database update --no-build 2>/dev/null || \
  docker exec misbahuda_api sh -c "cd /app && dotnet Misbahuda.API.dll migrate" 2>/dev/null || \
  echo "  (Migrations run automatically on startup — skipping manual step)"

# 5. Setup nginx (only first time)
if [ ! -f "/etc/nginx/sites-available/misbahulhoda" ]; then
  echo "[5/6] Setting up Nginx..."
  cp "$APP_DIR/scripts/nginx-server.conf" /etc/nginx/sites-available/misbahulhoda
  ln -sf /etc/nginx/sites-available/misbahulhoda /etc/nginx/sites-enabled/misbahulhoda
  nginx -t && systemctl reload nginx
  echo "  Nginx configured. Run: sudo certbot --nginx -d $DOMAIN"
else
  echo "[5/6] Nginx already configured — reloading..."
  nginx -t && systemctl reload nginx
fi

# 6. Status check
echo "[6/6] Container status:"
docker compose ps

echo ""
echo "=== Done! ==="
echo "App:   https://$DOMAIN"
echo "Admin: https://$DOMAIN/admin"
echo "API:   https://$DOMAIN/api/swagger"
echo ""
echo "First time? Run SSL setup:"
echo "  sudo apt install certbot python3-certbot-nginx -y"
echo "  sudo certbot --nginx -d $DOMAIN"
