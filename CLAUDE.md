# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

PolyglotPipeline is a learning/demo project for exploring how different data and messaging
technologies interoperate: MongoDB, RabbitMQ, Kafka, Redis, Elasticsearch, Docker, and (later)
Kubernetes. The domain is intentionally trivial (submit a piece of text, search for it back) so the
focus stays on the plumbing between technologies rather than business logic. RabbitMQ and Kafka are
deliberately kept as interchangeable, equally-supported brokers (picked per-request) rather than one
being primary — that's the point of comparison.

The solution (`PolyglotPipeline.sln`) has all five projects scaffolded as empty shells (default
templates, no real logic yet). See Plan.md for the step-by-step build order for everything else
(Mongo/RabbitMQ/Kafka/Redis/Elasticsearch wiring, docker-compose, Kubernetes).

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
- Update this file's "Commands" section once real commands exist (solution scaffolded, compose file
  added, etc.) instead of leaving them undocumented.

## Architecture (planned)

Solution: `PolyglotPipeline.sln`, stack: .NET (C#).

- **Shared.Contracts** — message contracts shared between publisher and consumers (e.g. a
  `TextSubmitted` event: Id, Text, CreatedAt).
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

- Local dev: `docker-compose.yml` (not yet added) running `mongo`, `mongo-express` (browser-based
  Mongo inspection, basic auth disabled), `rabbitmq` (`-management` image variant for its built-in
  browser UI), `kafka` (KRaft mode — no separate ZooKeeper container), `kafka-ui` (basic auth
  disabled — Kafka has no built-in UI), `redis`, `redis-commander` (basic auth disabled — Redis has
  no built-in UI either), `elasticsearch` (its `_search` REST endpoint is itself browser-GET-able,
  so no extra UI container needed), and the four .NET services (`ingest-api`, `search-api`,
  `redis-indexer`, `elastic-indexer`).
- Later: the same services deployed to a local Kubernetes cluster (e.g. minikube or kind) — manifests
  to be added under `k8s/` once the docker-compose stage works end-to-end.

## Commands

- `dotnet build PolyglotPipeline.sln` — build the whole solution (targets net10.0).
- `dotnet run --project src/Ingest.Api` (or `Search.Api`, `RedisIndexer.Worker`,
  `ElasticIndexer.Worker`) — run a single project.
- `dotnet test` — not yet applicable, no test projects exist.
- `docker compose up -d` / `docker compose down` — not yet applicable, no `docker-compose.yml` yet.
- `kubectl` commands — not yet applicable, Kubernetes phase hasn't started.
