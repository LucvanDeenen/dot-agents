#!/usr/bin/env bash
# Container entrypoint: materialize the run config, optionally clone the task's
# repo, record the working directory, then idle so the workspace and the Claude
# Code session persist. The API drives each turn (first turn + follow-ups) via
# `docker exec run.sh`; this process just keeps the run alive until its lifetime
# expires.
set -uo pipefail

HOME_DIR="${HOME:-/home/agent}"
WORKSPACE="$HOME_DIR/workspace"
cd "$WORKSPACE"

node "$HOME_DIR/setup.mjs" || exit 1

REPO_URL="$(cat "$HOME_DIR/repo-url.txt")"
REPO_BRANCH="$(cat "$HOME_DIR/repo-branch.txt")"
WORKDIR="$WORKSPACE"
if [[ -n "$REPO_URL" ]]; then
    CLONE_ARGS=(--depth 1)
    [[ -n "$REPO_BRANCH" ]] && CLONE_ARGS+=(--branch "$REPO_BRANCH")
    if git clone "${CLONE_ARGS[@]}" "$REPO_URL" repo 2>&1; then
        # The session runs inside the repo; keep the team/skill config visible
        # by linking the workspace .claude into it.
        [[ -e repo/.claude ]] || ln -s "$WORKSPACE/.claude" repo/.claude
        WORKDIR="$WORKSPACE/repo"
    else
        echo "warning: could not clone $REPO_URL — continuing in empty workspace" >&2
    fi
fi

# run.sh cd's here for every turn so `claude --continue` finds the same session.
echo "$WORKDIR" > "$HOME_DIR/workdir.txt"

MAX_LIFETIME_SECONDS="${AGENT_MAX_LIFETIME_SECONDS:-3600}"
echo "runner ready in $WORKDIR; idling up to ${MAX_LIFETIME_SECONDS}s" >&2
exec sleep "$MAX_LIFETIME_SECONDS"
