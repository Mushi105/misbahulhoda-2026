#!/bin/bash
# ── iOS Deployment Script ──────────────────────────────────────
# Builds web app, syncs to iOS, opens Xcode for Archive & Upload
# Usage: ./scripts/deploy-ios.sh

set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "▶ Step 1: Building web app for production..."
cd "$ROOT/web"
npm run build

echo "▶ Step 2: Syncing to iOS project..."
cd "$ROOT/mobile"
npx cap sync ios

echo "▶ Step 3: Opening Xcode..."
echo "   In Xcode: Product → Archive → Distribute App → App Store Connect"
npx cap open ios
