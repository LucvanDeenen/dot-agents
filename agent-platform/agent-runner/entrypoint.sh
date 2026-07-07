#!/bin/bash
set -euo pipefail

# Load the long-lived OAuth token (from `claude setup-token`) out of the
# read-only credentials mount rather than baking it into `docker create` Env —
# keeps it out of `docker inspect` output.
if [ -f /credentials/token ]; then
  export CLAUDE_CODE_OAUTH_TOKEN
  CLAUDE_CODE_OAUTH_TOKEN="$(cat /credentials/token)"
fi

# Make sure a stray ANTHROPIC_API_KEY never silently redirects billing away
# from the subscription.
unset ANTHROPIC_API_KEY || true

if [ -n "${REPO_URL:-}" ]; then
  git clone "$REPO_URL" /workspace/repo
  cd /workspace/repo
  if [ -n "${BRANCH:-}" ]; then
    git checkout -b "$BRANCH"
  fi
fi

# TODO: after the agent finishes, decide on your commit/push policy here —
# e.g. only push if `claude` exits 0 and there's a diff, always with
# `--author` / a Co-authored-by trailer, never straight to main without review.

claude -p "$PROMPT" \
  --output-format json \
  --allowedTools "${ALLOWED_TOOLS:-Read,Edit,Bash}" \
  --max-turns "${MAX_TURNS:-15}"
