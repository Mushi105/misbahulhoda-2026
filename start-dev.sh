#!/bin/bash

echo "🚀 Starting Misbahuda Development Environment"
echo ""

# Start PostgreSQL + Redis
echo "📦 Starting PostgreSQL and Redis..."
docker compose -f docker-compose.dev.yml up -d
echo "⏳ Waiting for PostgreSQL to be ready..."
sleep 5

# Run migrations
echo "🗄️  Running database migrations..."
dotnet ef database update \
  --project backend/src/Misbahuda.Infrastructure \
  --startup-project backend/src/Misbahuda.API

# Start API
echo "⚙️  Starting API on http://localhost:5025 ..."
dotnet run --project backend/src/Misbahuda.API &
API_PID=$!

# Start Frontend
echo "🌐 Starting Frontend on http://localhost:5173 ..."
cd frontend && npm run dev &
FRONTEND_PID=$!

echo ""
echo "✅ Misbahuda is running!"
echo "   Frontend : http://localhost:5173"
echo "   API      : http://localhost:5025"
echo "   Swagger  : http://localhost:5025/swagger"
echo ""
echo "   Admin Login: superadmin@misbahuda.com / Admin@2026!"
echo ""
echo "Press Ctrl+C to stop all services"

wait $API_PID $FRONTEND_PID
