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

Phase 0 (scaffolding) and Phase 1 (`Ingest.Api` + MongoDB) are done, and Phase 2 (messaging
publish) is in progress: `Ingest.Api` writes to MongoDB and publishes a `TextSubmitted` message to
either RabbitMQ or Kafka, selected per-request. `Search.Api`, `RedisIndexer.Worker`, and
`ElasticIndexer.Worker` are still empty template shells — nothing consumes these messages yet.

- `src/Shared.Contracts` — the shared `TextSubmitted` message contract (Id, Text, CreatedAt).
- `src/Ingest.Api` — `GET /ingest?text=...&broker=rabbitmq|kafka` stores `{ id, text, createdAt }`
  in MongoDB, publishes `TextSubmitted` to the selected broker, and returns the stored document.
- `src/Search.Api` — default ASP.NET Core minimal API template (`GET /` → "Hello World!").
- `src/RedisIndexer.Worker` — default Worker Service template (logs a heartbeat once a second).
- `src/ElasticIndexer.Worker` — default Worker Service template (logs a heartbeat once a second).

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) or later.
- [Docker](https://www.docker.com/) (for running via `docker-compose.yml`).

## Building

```
dotnet build PolyglotPipeline.sln
```

## Running via Docker Compose

```
docker compose up -d --build
```

This starts:

- `mongo` — MongoDB, at `localhost:27017`.
- `mongo-express` — browser-based Mongo admin UI (no login required): http://localhost:8081
- `rabbitmq` — AMQP at `localhost:5672`; management UI (guest/guest): http://localhost:15672
- `kafka` — broker at `localhost:9092` (KRaft mode, no ZooKeeper).
- `ingest-api` — http://localhost:8080

Hit `http://localhost:8080/ingest?text=hello&broker=rabbitmq` (or `broker=kafka`) in a browser.
Check `mongo-express` (database `polyglotpipeline`, collection `texts`) to see the Mongo write, and
the RabbitMQ management UI's `text-submitted` exchange to see the published message. Kafka doesn't
have a browser UI yet (that's a later Plan.md task) — inspect it with a console consumer:
`docker exec -it polyglotpipeline-kafka-1 /opt/kafka/bin/kafka-console-consumer.sh --bootstrap-server localhost:9092 --topic text-submitted --from-beginning`.

`docker compose down` stops and removes all of this project's containers plus the network (add `-v`
to also delete the `mongo-data` volume).

### Stopping just one container

```
docker compose stop mongo-express       # stop it, keep the container (docker compose start mongo-express to resume)
docker compose rm -s -f mongo-express   # stop (if running) and remove it entirely
```

### Inspecting/removing volumes

```
docker volume ls --filter name=polyglotpipeline    # this project's volumes (currently just mongo-data)
docker volume inspect polyglotpipeline_mongo-data  # details: mountpoint, size, etc.
docker volume rm polyglotpipeline_mongo-data        # remove it (containers using it must be down first)
```

`mongo-data` persists across `docker compose down`/`up`, so ingested test data (e.g. from
`?text=hello`) accumulates until you remove the volume.

## Debugging Ingest.Api in its container (VS Code)

The Dockerfile has a `debug` build target (Debug config + `vsdbg` baked in, kept separate from the
normal lean `runtime` target). To use it:

```
docker compose -f docker-compose.yml -f docker-compose.debug.yml up -d --build
```

Then in VS Code, Run and Debug → **"Docker: Attach to Ingest.Api"** (`.vscode/launch.json`) — it
attaches to the `dotnet` process inside `polyglotpipeline-ingest-api-1` via `docker exec`, so
breakpoints in `src/Ingest.Api` work as normal. Switch back to `docker compose up -d --build`
(without the debug override) for the regular lean image.

## Running a project directly

```
dotnet run --project src/Ingest.Api
dotnet run --project src/Search.Api
dotnet run --project src/RedisIndexer.Worker
dotnet run --project src/ElasticIndexer.Worker
```

`Ingest.Api` defaults to `mongodb://localhost:27017` (see `appsettings.json`), so a local or
Dockerized Mongo needs to be reachable at that address if you run it this way instead of via
Compose. `Search.Api` and the two workers are still just default templates — see Plan.md for what's
next.
