#!/bin/bash
# ── Local Development ─────────────────────────────────────────
# Starts backend + web dev server together
# Usage: ./scripts/dev.sh

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "▶ Starting backend..."
cd "$ROOT/backend/src/Misbahuda.API"
dotnet run --launch-profile http &
BACKEND_PID=$!

echo "▶ Starting web dev server..."
cd "$ROOT/web"
npm run dev &
WEB_PID=$!

echo ""
echo "✓ Running:"
echo "  Backend:  http://0.0.0.0:5025"
echo "  Web:      http://localhost:5173"
echo "  Mobile:   Load app in simulator (reads from http://192.168.100.225:5173)"
echo ""
echo "Press Ctrl+C to stop all..."

trap "kill $BACKEND_PID $WEB_PID 2>/dev/null; exit" INT
wait
