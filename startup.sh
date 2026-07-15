#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ── Infra (docker-compose) ────────────────────────────────────────────────────
echo "Starting infra services..."
docker compose -f "$SCRIPT_DIR/infra/docker-compose.yml" --env-file "$SCRIPT_DIR/infra/.env" up -d

echo ""
echo "Waiting for infra services to be healthy..."
for service in postgres rabbitmq; do
    container=$(docker compose -f "$SCRIPT_DIR/infra/docker-compose.yml" --env-file "$SCRIPT_DIR/infra/.env" ps -q "$service")
    echo -n "  $service: "
    until [ "$(docker inspect --format='{{.State.Health.Status}}' "$container" 2>/dev/null)" = "healthy" ]; do
        echo -n "."
        sleep 2
    done
    echo " healthy"
done
echo ""

# ── Backend (.NET API) ────────────────────────────────────────────────────────
echo "Starting backend (dotnet run)..."
pushd "$SCRIPT_DIR/agent-platform/api" > /dev/null
dotnet run --launch-profile http &
BACKEND_PID=$!
popd > /dev/null

# ── Frontend (Vite dev server) ────────────────────────────────────────────────
echo "Starting frontend (npm run dev)..."
pushd "$SCRIPT_DIR/frontend" > /dev/null
npm run dev &
FRONTEND_PID=$!
popd > /dev/null

echo ""
echo "All services started:"
echo "  Infra     : docker-compose (postgres + rabbitmq)"
echo "  Backend   : http://localhost:5005  (PID $BACKEND_PID)"
echo "  Frontend  : http://localhost:3000  (PID $FRONTEND_PID)"
echo ""
echo "Press Ctrl+C to stop the backend and frontend."
echo ""

# ── Cleanup on exit ───────────────────────────────────────────────────────────
cleanup() {
    echo ""
    echo "Stopping backend and frontend..."
    kill "$BACKEND_PID" "$FRONTEND_PID" 2>/dev/null || true
    wait "$BACKEND_PID" "$FRONTEND_PID" 2>/dev/null || true
    echo "Done. Infra containers are still running (use 'docker compose -f infra/docker-compose.yml down' to stop them)."
}
trap cleanup EXIT INT TERM

wait "$BACKEND_PID" "$FRONTEND_PID"
