#!/usr/bin/env bash
# Keep the container alive so the API can drive Claude turns via `docker exec`
# (and `claude --continue` can resume the session). Self-terminate after the
# configured max lifetime.
set -uo pipefail

# ── Git identity + credentials (optional) ───────────────────────────────────
# When a token is provided, configure git so the agent can push over HTTPS
# without an interactive prompt. Values arrive as env vars from the API.
git config --global user.name  "${GIT_USER_NAME:-Agent Platform}"
git config --global user.email "${GIT_USER_EMAIL:-agent@agent-platform.local}"

if [[ -n "${GIT_TOKEN:-}" ]]; then
    GIT_HOST="${GIT_HOST:-github.com}"
    git config --global credential.helper store
    # x-access-token works for GitHub PATs; the store helper matches by host.
    printf 'https://x-access-token:%s@%s\n' "$GIT_TOKEN" "$GIT_HOST" > "$HOME/.git-credentials"
    chmod 600 "$HOME/.git-credentials"
fi

MAX_LIFETIME_SECONDS="${AGENT_MAX_LIFETIME_SECONDS:-3600}"
exec sleep "$MAX_LIFETIME_SECONDS"
