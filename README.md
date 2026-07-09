# dot-agents

Self-hosted agentic setup for managing my own agents at home (runs on a Raspberry Pi).

- **frontend/** — Vue app for submitting/interacting with tasks.
- **agent-platform/api/** — ASP.NET Core API. Accepts task requests, persists them to Postgres, and publishes them onto a RabbitMQ topic exchange so worker agents can pick them up.

## Architecture

- **postgres** — task/job state store.
- **rabbitmq** — topic exchange (`agent.tasks`) that task requests are published to; queues bound to it by routing key are consumed by worker agents that pick up and process the work (currently: development tasks that commit into GitHub repos).
- **agent-platform/api** — connects to both on startup: runs EF Core migrations against Postgres and declares the RabbitMQ topology (exchange/queue/binding).

## Local development

```bash
cp .env.example .env   # fill in real credentials
docker compose up -d   # postgres + rabbitmq
cd agent-platform/api
dotnet run
```

RabbitMQ management UI: http://localhost:15673 (or whatever port you mapped in `docker-compose.yml` — 5672/15672 are remapped to 5673/15673 by default to avoid clashing with other local RabbitMQ instances).

## Notes / TODO

- `AgentTask` (Postgres table) and the `agent.tasks` exchange/`agent.tasks.queue` binding are placeholders — routing key scheme (`task.#`), per-agent-type queues, and the actual publish/consume + agent dispatch logic still need to be built.
- No auth on the API yet.
