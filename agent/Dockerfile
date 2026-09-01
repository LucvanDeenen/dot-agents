# Per-run agent container: a Claude Code session authenticated with a Claude
# subscription token (CLAUDE_CODE_OAUTH_TOKEN). The container idles; the API
# drives each turn with `docker exec claude -p …`. Build with:
#   docker build -t agent-runner:local agent/
FROM node:22-slim

# ca-certificates: TLS to the Claude API. ripgrep: Claude Code's file search.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates ripgrep \
    && rm -rf /var/lib/apt/lists/*

RUN npm install -g @anthropic-ai/claude-code

# Claude Code refuses to run with elevated permission modes as root.
RUN useradd -m agent
USER agent
WORKDIR /home/agent/workspace

COPY --chown=agent:agent entrypoint.sh /home/agent/entrypoint.sh

ENTRYPOINT ["/bin/bash", "/home/agent/entrypoint.sh"]
