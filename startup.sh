#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ── Mode argument ─────────────────────────────────────────────────────────────
usage() {
    echo "Usage: $0 <all|infra|frontend|backend>"
    echo "  all      - start infra, backend, and frontend"
    echo "  infra    - start infra only"
    echo "  frontend - start infra and frontend only"
    echo "  backend  - start infra and backend only"
}

if [[ $# -lt 1 ]]; then
    usage
    exit 1
fi

MODE="$1"
case "$MODE" in
  all|infra|frontend|backend) ;;
  *)
    usage
    exit 1
    ;;
esac

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

if [[ "$MODE" == "infra" ]]; then
    echo "Infra services started (mode: infra):"
    echo "  Infra     : docker-compose (postgres + rabbitmq)"
    echo ""
    echo "Use './stop.sh' to stop them."
    exit 0
fi

PIDS=()

# ── Kill any process already occupying a port ─────────────────────────────────
kill_port() {
    local port="$1"
    local pid=""
    if command -v lsof &>/dev/null; then
        pid=$(lsof -ti tcp:"$port" 2>/dev/null | head -1 || true)
    elif command -v netstat &>/dev/null; then
        pid=$(netstat -ano 2>/dev/null \
              | grep -E "[:.]${port}[[:space:]].*LISTENING" \
              | awk '{print $NF}' | head -1 || true)
    fi
    if [[ -n "$pid" && "$pid" != "0" ]]; then
        echo "  Port $port in use by PID $pid — killing..."
        kill "$pid" 2>/dev/null || taskkill //F //PID "$pid" 2>/dev/null || true
        sleep 1
    fi
}

# ── Backend (.NET API) ────────────────────────────────────────────────────────
if [[ "$MODE" == "all" || "$MODE" == "backend" ]]; then
    kill_port 5005
    # Expose infra/.env values (notably CLAUDE_CODE_OAUTH_TOKEN) to the API so
    # the Docker agent runner can pass the token into agent containers.
    set -a; source "$SCRIPT_DIR/infra/.env"; set +a
    echo "Starting backend (dotnet run)..."
    pushd "$SCRIPT_DIR/backend/api" > /dev/null
    dotnet run --launch-profile http &
    BACKEND_PID=$!
    PIDS+=("$BACKEND_PID")
    popd > /dev/null
fi

# ── Frontend (Vite dev server) ────────────────────────────────────────────────
if [[ "$MODE" == "all" || "$MODE" == "frontend" ]]; then
    kill_port 3000
    echo "Starting frontend (npm run dev)..."
    pushd "$SCRIPT_DIR/frontend" > /dev/null
    npm run dev &
    FRONTEND_PID=$!
    PIDS+=("$FRONTEND_PID")
    popd > /dev/null
fi

echo ""
echo "Services started (mode: $MODE):"
echo "  Infra     : docker-compose (postgres + rabbitmq)"
[[ "$MODE" == "all" || "$MODE" == "backend" ]]  && echo "  Backend   : http://localhost:5005  (PID $BACKEND_PID)"
[[ "$MODE" == "all" || "$MODE" == "frontend" ]] && echo "  Frontend  : http://localhost:3000  (PID $FRONTEND_PID)"
echo ""
echo "Press Ctrl+C to stop."
echo ""

# ── Cleanup on exit ───────────────────────────────────────────────────────────
cleanup() {
    trap - EXIT INT TERM
    echo ""
    echo "Stopping processes..."
    kill "${PIDS[@]}" 2>/dev/null || true
    wait "${PIDS[@]}" 2>/dev/null || true
    echo "Done. Infra containers are still running (use './stop.sh' to stop them)."
}
trap cleanup EXIT INT TERM

wait "${PIDS[@]}"
