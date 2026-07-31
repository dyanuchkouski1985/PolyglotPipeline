# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

PolyglotPipeline is a learning/demo project for exploring how different data and messaging
technologies interoperate: MongoDB, RabbitMQ, Redis, Elasticsearch, Docker, and (later) Kubernetes.
The domain is intentionally trivial (submit a piece of text, search for it back) so the focus stays
on the plumbing between technologies rather than business logic.

No code has been scaffolded yet — see Plan.md for the step-by-step build order.

## Ground rules

- No authentication anywhere, on any endpoint. Every endpoint is a plain GET so it can be exercised
  directly from a browser address bar.
- Build one Plan.md step at a time. The user reviews and commits each step to git themselves to
  study the diff, so do not bundle multiple Plan.md steps into a single change unless asked to.
- Update Plan.md as steps are completed or re-scoped — it is the authoritative task list.
- Update this file's "Commands" section once real commands exist (solution scaffolded, compose file
  added, etc.) instead of leaving them undocumented.

## Architecture (planned)

Solution: `PolyglotPipeline.sln`, stack: .NET (C#).

- **Shared.Contracts** — message contracts shared between publisher and consumers (e.g. a
  `TextSubmitted` event: Id, Text, CreatedAt).
- **Ingest.Api** — ASP.NET Core minimal API. `GET /ingest?text=...` writes the text to MongoDB, then
  publishes a `TextSubmitted` message to RabbitMQ.
- **RedisIndexer.Worker** — BackgroundService that consumes `TextSubmitted` from RabbitMQ and writes
  it to Redis.
- **ElasticIndexer.Worker** — BackgroundService that consumes `TextSubmitted` from RabbitMQ (same
  message, its own queue/binding) and indexes it into Elasticsearch.
- **Search.Api** — ASP.NET Core minimal API with three independent GET endpoints, one per store:
  `/search/mongo?q=...`, `/search/redis?q=...`, `/search/elastic?q=...`. Each queries its own store
  directly — there is no cross-store fallback or merging, since the point is to compare the stores
  side by side.

Data flow: `Ingest.Api` → Mongo (write) + RabbitMQ (publish) → fan-out to two independent consumers →
Redis / Elasticsearch (write). The read side never touches RabbitMQ — `Search.Api` reads directly
from each store.

## Infrastructure

- Local dev: `docker-compose.yml` (not yet added) running `mongo`, `rabbitmq`, `redis`,
  `elasticsearch`, and the four .NET services (`ingest-api`, `search-api`, `redis-indexer`,
  `elastic-indexer`).
- Later: the same services deployed to a local Kubernetes cluster (e.g. minikube or kind) — manifests
  to be added under `k8s/` once the docker-compose stage works end-to-end.

## Commands

Not yet applicable — no solution has been scaffolded. Once `PolyglotPipeline.sln` exists, fill in:
- `dotnet build` / `dotnet test` for the whole solution, and how to run a single project
  (`dotnet run --project src/...`).
- `docker compose up -d` / `docker compose down` for local infra + services.
- Any `kubectl` commands once the Kubernetes phase starts.
