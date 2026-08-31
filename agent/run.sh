#!/usr/bin/env bash
# Runs a single Claude Code turn and prints its JSON result on stdout. Invoked by
# the API via `docker exec` once per turn:
#   run.sh "<message>" start      → first turn; empty message uses the guide prompt
#   run.sh "<message>" continue   → follow-up; resumes the most recent session
# The workspace/session flags are the .txt files setup.mjs materialized.
set -uo pipefail

HOME_DIR="${HOME:-/home/agent}"
MESSAGE="${1:-}"
MODE="${2:-start}"

cd "$(cat "$HOME_DIR/workdir.txt")" || exit 1

# First turn with no explicit message → use the framed guide prompt.
if [[ -z "$MESSAGE" ]]; then
    MESSAGE="$(cat "$HOME_DIR/guide-prompt.txt")"
fi

ARGS=(-p "$MESSAGE" --output-format json --dangerously-skip-permissions)

# Resume the existing conversation for follow-up turns.
[[ "$MODE" == "continue" ]] && ARGS+=(--continue)

SYSTEM_PROMPT="$(cat "$HOME_DIR/system-prompt.txt")"
[[ -n "$SYSTEM_PROMPT" ]] && ARGS+=(--append-system-prompt "$SYSTEM_PROMPT")

ALLOWED_TOOLS="$(cat "$HOME_DIR/allowed-tools.txt")"
[[ -n "$ALLOWED_TOOLS" ]] && ARGS+=(--allowedTools "$ALLOWED_TOOLS")

exec claude "${ARGS[@]}"
