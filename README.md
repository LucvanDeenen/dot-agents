# dot-agents

A self-hosted agentic platform: plug in your Claude subscription, define agents (skills, tools, context), and let them pick up tasks from a queue. Runs at home (target: Raspberry Pi / any Docker host).

## Goal

> "A platform that allows me to plug in and utilize my Claude subscription to set up agents that can pick up tasks to do from a queue."

Long term this should become an easy-to-use agentic platform where people can either (a) plug in / configure their own agents to pick up work, or (b) consume already-defined agents as a service. The current milestone is the **proof of concept**: the four core systems below, working end-to-end.

## Core systems (POC scope)

| # | System | What it does | Where it lives |
|---|--------|--------------|----------------|
| 1 | **Frontend** | Sidebar app with three pages: **Tasks** (create/submit work), **Agents** (define agents: skills, allowed tools, context/system prompt), **Board** (kanban reflecting live task status via SSE). | `frontend/` (Vue 3 + Tailwind) |
| 2 | **Task queue** | RabbitMQ topic exchange `agent.tasks`; tasks are published with a routing key and consumed by the dispatcher. | `infra/docker-compose.yml` |
| 3 | **API + dispatcher** | ASP.NET Core service. Persists agents/skills/tasks to Postgres, publishes tasks onto RabbitMQ, and consumes them again: when capacity is available and an agent's routing pattern matches the task, it triggers an agent run. Also the store for all defined skills. | `agent-platform/` |
| 4 | **Agent runner** | Docker-spawned containers (capped concurrency) that run a Claude Code session per task using your Claude subscription. Each run is a minimal two-agent team: the main session acts as the **guide** (plans, reviews, reports) and delegates implementation to a **completer** subagent generated from the agent's skills. | `agent-runner/` + `DockerAgentRunner` in the API |

### Design decisions (locked for the POC)

- **Execution**: the dispatcher talks to the Docker Engine API and starts one `agent-runner` container per picked-up task, capped at `AgentRunner:MaxConcurrency` (default 2). No container = no idle cost; the cap is the "auto-scaling" story at POC size.
- **Claude auth**: containers authenticate with a long-lived subscription token (`claude setup-token`) passed as `CLAUDE_CODE_OAUTH_TOKEN`. No API key required.
- **Two-agent team**: one Claude Code session per task; the guide/completer split is realized with Claude Code's native subagent mechanism (a generated `.claude/agents/completer.md`), not two separate processes.
- **Agent model**: `Agent` = name, description, system prompt (context), allowed tools, routing-key pattern it consumes. `Skill` is a first-class reusable entity (name, description, instructions) that agents reference (many-to-many); an agent's skills are materialized into the runner container as Claude Code skills.
- **Dispatcher location**: stays inside the API process (`TaskQueueListener`) — one deployable. Splitting it into its own worker service is a post-POC step.

### Task lifecycle

```
frontend POST /jobs ──► Postgres row (Pending) ──► publish to agent.tasks (Queued)
        ▲                                                    │
        │ SSE /jobs/events                                   ▼
        └──────────── status updates ◄── dispatcher consumes, matches agent,
                                          waits for a free slot, starts
                                          agent-runner container (Running)
                                                             │
                                          container exit 0 ─► Completed
                                          container exit ≠0 ─► Failed
```

## Out of scope (post-POC)

- Auth, end to end. The API is open, and the frontend's `auth-client` plugin was
  registered but unused (and pulled a private repo over git+ssh that CI can't
  clone), so it was removed until auth is actually implemented. Deployed access
  is gated by the Cloudflare tunnel only.
- Multi-tenancy / other users plugging in their own subscriptions.
- Real horizontal auto-scaling (Kubernetes, multiple hosts) and a separate dispatcher service.
- Dead-letter queue / retry policies beyond the current requeue behaviour.
- Per-agent-type queues (single queue + routing-pattern matching in the dispatcher for now).

## Repo layout

- `frontend/` — Vue app (Tasks / Agents / Board).
- `agent-platform/` — .NET solution: `api` (spec-first controllers via NSwag — edit `api/Spec/agent-platform.yaml`, not the generated code), `application` (MediatR handlers), `domain`, `infrastructure` (EF Core + RabbitMQ + Docker runner).
- `agent-runner/` — Dockerfile + entrypoint for the per-task Claude Code container.
- `infra/` — docker-compose for Postgres + RabbitMQ (local development only).
- `.github/workflows/publish.yml` — builds and pushes the three deployable images.

## Local development

```bash
cp infra/.env.example infra/.env   # fill in real credentials + CLAUDE_CODE_OAUTH_TOKEN
./startup.sh                       # infra (docker compose) + API + frontend

# build the agent runner image once (required for real agent runs):
docker build -t agent-runner:local agent-runner/
```

- Frontend: http://localhost:3000 — API: http://localhost:5005
- RabbitMQ management UI: http://localhost:15673 (AMQP on 5673; remapped from 5672/15672 to avoid clashes).
- Get a subscription token with `claude setup-token` and put it in `infra/.env` as `CLAUDE_CODE_OAUTH_TOKEN`.

## Deployment (Raspberry Pi)

Images are built for `linux/amd64` + `linux/arm64` and pushed to GHCR on every
push to `main` (or via **Run workflow** manually — note that only `main` gets
the `:latest` tag the Pi pulls):

| Image | Purpose |
|-------|---------|
| `ghcr.io/lucvandeenen/dot-agents/agent-platform-api` | API + dispatcher |
| `ghcr.io/lucvandeenen/dot-agents/frontend` | Vue app behind nginx (also proxies the API) |
| `ghcr.io/lucvandeenen/dot-agents/agent-runner` | Per-task Claude Code container |

The Pi-side stack lives in the homelab config repo at `dot-conf/infra/dot-agents/`
(Postgres, RabbitMQ, API, frontend; Traefik-routed at `agents.dedeen.dev`) and is
deployed by `dot-infra/install.sh`. Setup steps — GHCR login, the Claude
subscription token, and the `.env` — are documented in that stack's `README.md`.

Two things worth knowing before deploying:

- The API mounts `/var/run/docker.sock` to spawn agent containers. That is
  root-equivalent access to the host, and an agent run executes arbitrary code.
- Agent containers are started as siblings on the host engine, so they are not
  part of the compose stack. Find them with
  `docker ps --filter label=agent-platform.task-id`.

## Roadmap

1. **POC (this milestone)** — the four systems above, single-node.
2. Split the dispatcher into its own worker service; per-agent queues bound by routing key.
3. Harden the runner: workspace mounting for repos, artifact/output capture, dead-letter queue.
4. Platformize: auth on the API, multi-user agent/skill libraries, pluggable subscriptions (long-term goals a & b).
