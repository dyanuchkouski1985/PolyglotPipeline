# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

PolyglotPipeline is a learning/demo project for exploring how different data and messaging
technologies interoperate: MongoDB, RabbitMQ, Kafka, Redis, Elasticsearch, Docker, and (later)
Kubernetes. The domain is intentionally trivial (submit a piece of text, search for it back) so the
focus stays on the plumbing between technologies rather than business logic. RabbitMQ and Kafka are
deliberately kept as interchangeable, equally-supported brokers (picked per-request) rather than one
being primary — that's the point of comparison.

The solution (`PolyglotPipeline.sln`) is built out incrementally per Plan.md's phases; not every
project has real logic yet (some are still default template shells). Check Plan.md for which phase
is checked off and README.md's "Current state" section for a plain-English summary of what's
actually implemented right now — don't infer build status from this file, it isn't kept in lockstep
with every phase.

## Ground rules

- No authentication anywhere, on any endpoint. Every endpoint is a plain GET so it can be exercised
  directly from a browser address bar. This extends to any admin/inspection UI added purely for
  local-dev convenience (e.g. `mongo-express`, `kafka-ui`, `redis-commander`) — disable its auth
  too, don't leave it as the one thing behind a login.
- Build one Plan.md step at a time. The user reviews and commits each step to git themselves to
  study the diff, so do not bundle multiple Plan.md steps into a single change unless asked to.
- Use the `/do-next` command (`.claude/commands/do-next.md`) to pick up and implement the next
  unchecked task from Plan.md — that's the intended workflow for building this project out.
- Update Plan.md as steps are completed or re-scoped — it is the authoritative task list.
- Use the latest stable version of any library, NuGet package, or Docker image a task needs (.NET
  SDK/runtime, client SDKs for Mongo/RabbitMQ/Kafka/Redis/Elasticsearch, base images in Dockerfiles,
  etc.) unless a specific version is called for — don't pin to an older version out of habit.
- RabbitMQ and Kafka are peers, not primary/fallback. `Ingest.Api` picks one per request via a
  `broker` query parameter; consumers must treat a message arriving from either broker identically
  — don't special-case handler logic per broker.
- Don't repeat a string literal that identifies a domain concept (collection name, broker name,
  queue/topic name, etc.) across more than one place in the same file/type. Define it once as a
  constant near its natural owner (e.g. `TextDocument.CollectionName`) and reference that instead.
- Update this file's "Commands" section once real commands exist (solution scaffolded, compose file
  added, etc.) instead of leaving them undocumented.

## Architecture

Solution: `PolyglotPipeline.sln`, stack: .NET (C#), targeting net10.0. This describes the target
shape of each service; see Plan.md/README.md for which pieces are actually wired up yet.

- **Shared.Contracts** — types shared across projects that would otherwise duplicate a domain
  concept: `TextSubmitted` (the broker message: Id, Text, CreatedAt), and `TextDocument` (the Mongo
  document shape + `CollectionName`, shared between `Ingest.Api`, which writes it, and `Search.Api`,
  which reads it back).
- **Ingest.Api** — ASP.NET Core minimal API. `GET /ingest?text=...&broker=rabbitmq|kafka` writes the
  text to MongoDB, then publishes a `TextSubmitted` message to whichever broker the request asked
  for. `broker` selects the transport only — it does not change what gets stored or published.
- **RedisIndexer.Worker** — BackgroundService running two independent listeners, one consuming
  `TextSubmitted` from a RabbitMQ queue and one from a Kafka topic, both feeding the same handler
  that writes to Redis. The handler doesn't know or care which broker delivered the message.
- **ElasticIndexer.Worker** — same dual-listener shape as `RedisIndexer.Worker` (its own RabbitMQ
  queue + Kafka topic/consumer group), feeding a shared handler that indexes into Elasticsearch.
- **Search.Api** — ASP.NET Core minimal API with three independent GET endpoints, one per store:
  `/search/mongo?q=...`, `/search/redis?q=...`, `/search/elastic?q=...`. Each queries its own store
  directly — there is no cross-store fallback or merging, since the point is to compare the stores
  side by side.

Data flow: `Ingest.Api` → Mongo (write) + broker publish, RabbitMQ or Kafka chosen per request →
fan-out to two independent consumers, each listening on both brokers → Redis / Elasticsearch (write).
The read side never touches a broker — `Search.Api` reads directly from each store.

## Infrastructure

- Local dev: `docker-compose.yml` runs `mongo`, `mongo-express` (browser-based Mongo inspection,
  basic auth disabled), `rabbitmq` (`-management` image variant for its built-in browser UI), `kafka`
  (KRaft mode — no separate ZooKeeper container), `kafka-ui` (basic auth disabled — Kafka has no
  built-in UI), `redis`, `redis-commander` (basic auth disabled — Redis has no built-in UI either),
  `elasticsearch` (security disabled via `xpack.security.enabled=false`; its `_search` REST endpoint
  is itself browser-GET-able, so no extra UI container is needed), `ingest-api`, `redis-indexer`, and
  `elastic-indexer` (neither indexer has a container port of its own — they only consume). Not yet in
  compose: `search-api` — it lands in a later Plan.md phase.
- `src/Ingest.Api/Dockerfile` has a `debug` build target alongside the normal lean `runtime` target
  (Debug config + `vsdbg` baked in) for attaching VS Code's debugger to the containerized process —
  see README.md's "Debugging Ingest.Api in its container" section. Follow the same pattern (separate
  `debug` target, `docker-compose.debug.yml` override) if other services need the same later.
- Later: the same services deployed to a local Kubernetes cluster (e.g. minikube or kind) — manifests
  to be added under `k8s/` once the docker-compose stage works end-to-end.

## Commands

- `dotnet build PolyglotPipeline.sln` — build the whole solution (targets net10.0).
- `dotnet run --project src/Ingest.Api` (or `Search.Api`, `RedisIndexer.Worker`,
  `ElasticIndexer.Worker`) — run a single project. All four default to `localhost` for whichever of
  Mongo/RabbitMQ/Kafka/Redis/Elasticsearch they use (see each project's `appsettings.json`), so those
  need to be reachable there if running this way instead of via Compose.
- `dotnet test` — not yet applicable, no test projects exist.
- `docker compose up -d --build` — build and start the full local stack (see Infrastructure above
  for the service list and ports/UIs). `docker compose down` stops them (add `-v` to also drop the
  `mongo-data`/`redis-data`/`elastic-data` volumes).
- `docker compose -f docker-compose.yml -f docker-compose.debug.yml up -d --build` — same stack, but
  `ingest-api` built from the `debug` Dockerfile target so VS Code can attach (`Docker: Attach to
  Ingest.Api` in `.vscode/launch.json`).
- `kubectl` commands — not yet applicable, Kubernetes phase hasn't started.
