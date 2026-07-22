#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "Stopping infra services..."
docker compose -f "$SCRIPT_DIR/infra/docker-compose.yml" --env-file "$SCRIPT_DIR/infra/.env" down

echo "Done."
