#!/usr/bin/env bash
# Keep the container alive so the API can drive Claude turns via `docker exec`
# (and `claude --continue` can resume the session). Self-terminate after the
# configured max lifetime.
set -uo pipefail
exec sleep "${AGENT_MAX_LIFETIME_SECONDS:-3600}"
