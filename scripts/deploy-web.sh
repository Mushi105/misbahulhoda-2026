#!/bin/bash
# ── Web Deployment Script ──────────────────────────────────────
# Builds Vue.js app and deploys to server via Docker or SCP
# Usage: ./scripts/deploy-web.sh

set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "▶ Building web app..."
cd "$ROOT/web"
npm run build

echo "▶ Web build ready at: $ROOT/web/dist"
echo ""
echo "Deploy options:"
echo "  Docker:  docker build -t misbahuda-web . && docker push misbahuda-web"
echo "  SCP:     scp -r dist/* user@yourserver:/var/www/misbahuda/"
echo ""
echo "✓ Web build complete."
