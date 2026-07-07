# agent-platform

Self-hosted agentic orchestration layer: a .NET API that accepts job requests and dispatches them as ephemeral Claude Code containers via a scoped docker-socket-proxy, backed by Postgres for job state.

## Architecture

- **agent-api** — ASP.NET Core minimal API. Accepts job requests, persists them, polls and dispatches pending jobs.
- **postgres** — job queue / state store. Internal network only.
- **docker-socket-proxy** — scoped Docker API access so agent-api can create containers without holding raw `docker.sock`.
- **agent-runner** (image, not a running service) — Node + Claude Code CLI image. agent-api launches one ephemeral container per job from this image, then removes it on completion.

## Secrets

- `.env` — **Required**. Postgres credentials (`POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`). Copy from `.env.example`.
- `claude-credentials` volume — **Required**. Populate once via `claude setup-token` on a machine with your subscription logged in, then copy the resulting token file into the volume. Mounted read-only into agent-api and passed through to each ephemeral agent-runner container.

## Build the agent-runner image

```bash
docker build -t agent-runner:latest ./agent-runner
```

Rebuild whenever you bump the Claude Code CLI version pinned in `agent-runner/Dockerfile`.

## Notes / TODO

- git checkout + co-authored commits are stubbed in `agent-runner/entrypoint.sh` — wire up your deploy key / repo access there.
- No auth on the API yet — put it behind something before exposing `agent.dedeen.dev` beyond your own use.
- `--max-turns` / `--allowedTools` are hardcoded in `ClaudeAgentRunner.cs` for now — worth making these per-job-type config.
