# PolyglotPipeline

A learning/demo project for exploring how different data and messaging technologies interoperate:
MongoDB, RabbitMQ, Kafka, Redis, Elasticsearch, Docker, and (later) Kubernetes. The domain is
intentionally trivial (submit a piece of text, search for it back) so the focus stays on the
plumbing between technologies rather than business logic.

See [CLAUDE.md](CLAUDE.md) for the planned architecture and ground rules, and [Plan.md](Plan.md)
for the step-by-step build order. This README is kept in sync with what's actually been built so
far, phase by phase — it will grow real run instructions (docker-compose, endpoints to hit) as each
phase lands.

## Current state

Only Phase 0 (scaffolding) is done: the solution and its five projects exist, but none of them have
real logic yet — no Mongo, RabbitMQ, Kafka, Redis, or Elasticsearch wiring, and no
`docker-compose.yml`. There's nothing meaningful to run end-to-end yet.

- `src/Shared.Contracts` — empty class library.
- `src/Ingest.Api` — default ASP.NET Core minimal API template (`GET /` → "Hello World!").
- `src/Search.Api` — default ASP.NET Core minimal API template (`GET /` → "Hello World!").
- `src/RedisIndexer.Worker` — default Worker Service template (logs a heartbeat once a second).
- `src/ElasticIndexer.Worker` — default Worker Service template (logs a heartbeat once a second).

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) or later.

## Building

```
dotnet build PolyglotPipeline.sln
```

## Running a project

```
dotnet run --project src/Ingest.Api
dotnet run --project src/Search.Api
dotnet run --project src/RedisIndexer.Worker
dotnet run --project src/ElasticIndexer.Worker
```

The two API projects print the URL they're listening on; the two worker projects just log a
heartbeat to the console. None of this reflects real functionality yet — see Plan.md for what's
next.
