# Per-task agent container: one Claude Code session per task, authenticated
# with a Claude subscription token (CLAUDE_CODE_OAUTH_TOKEN). The task + agent
# definition arrive as base64 JSON in RUN_CONFIG; setup.mjs materializes them
# into a workspace (guide prompt, completer subagent, skills) and
# entrypoint.sh runs the session. Build with:
#   docker build -t agent-runner:local agent-runner/
FROM node:22-slim

RUN apt-get update \
    && apt-get install -y --no-install-recommends git ca-certificates curl ripgrep \
    && rm -rf /var/lib/apt/lists/*

RUN npm install -g @anthropic-ai/claude-code

# Claude Code refuses to run with elevated permission modes as root.
RUN useradd -m agent
USER agent
WORKDIR /home/agent/workspace

COPY --chown=agent:agent setup.mjs /home/agent/setup.mjs
COPY --chown=agent:agent entrypoint.sh /home/agent/entrypoint.sh

ENTRYPOINT ["/bin/bash", "/home/agent/entrypoint.sh"]
