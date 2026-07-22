#!/usr/bin/env bash
# Container entrypoint: materialize the run config, optionally clone the
# task's repo, then run the guide session. The session's stdout is the task
# output; the exit code decides Completed vs Failed.
set -uo pipefail

HOME_DIR="${HOME:-/home/agent}"
WORKSPACE="$HOME_DIR/workspace"
cd "$WORKSPACE"

node "$HOME_DIR/setup.mjs" || exit 1

REPO_URL="$(cat "$HOME_DIR/repo-url.txt")"
REPO_BRANCH="$(cat "$HOME_DIR/repo-branch.txt")"
if [[ -n "$REPO_URL" ]]; then
    CLONE_ARGS=(--depth 1)
    [[ -n "$REPO_BRANCH" ]] && CLONE_ARGS+=(--branch "$REPO_BRANCH")
    if git clone "${CLONE_ARGS[@]}" "$REPO_URL" repo 2>&1; then
        cd repo
        # The guide session runs inside the repo; keep the team/skill config
        # visible by linking the workspace .claude into it.
        [[ -e .claude ]] || ln -s "$WORKSPACE/.claude" .claude
    else
        echo "warning: could not clone $REPO_URL — continuing in empty workspace" >&2
    fi
fi

CLAUDE_ARGS=(
    -p "$(cat "$HOME_DIR/guide-prompt.txt")"
    --output-format text
    --dangerously-skip-permissions
)

SYSTEM_PROMPT="$(cat "$HOME_DIR/system-prompt.txt")"
[[ -n "$SYSTEM_PROMPT" ]] && CLAUDE_ARGS+=(--append-system-prompt "$SYSTEM_PROMPT")

ALLOWED_TOOLS="$(cat "$HOME_DIR/allowed-tools.txt")"
[[ -n "$ALLOWED_TOOLS" ]] && CLAUDE_ARGS+=(--allowedTools "$ALLOWED_TOOLS")

exec claude "${CLAUDE_ARGS[@]}"
