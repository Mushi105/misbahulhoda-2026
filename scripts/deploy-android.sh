#!/bin/bash
# ── Android Deployment Script ─────────────────────────────────
# Builds web app, syncs to Android, opens Android Studio for APK/AAB
# Usage: ./scripts/deploy-android.sh

set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "▶ Step 1: Building web app for production..."
cd "$ROOT/web"
npm run build

echo "▶ Step 2: Adding Android project (first time only)..."
cd "$ROOT/mobile"
if [ ! -d "android" ]; then
  npx cap add android
  echo "   Android project created."
fi

echo "▶ Step 3: Syncing to Android project..."
npx cap sync android

echo "▶ Step 4: Opening Android Studio..."
echo "   In Android Studio: Build → Generate Signed APK / Bundle"
npx cap open android
