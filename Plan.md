# Plan.md

Step-by-step build order for PolyglotPipeline. Each phase is meant to be one (or a few) git commits
the user makes and reviews themselves before moving on. Do not jump ahead to a later phase or
implement multiple phases in one pass unless explicitly asked. Check items off as they land.

## Phase 0 — Scaffolding

- [ ] Create `PolyglotPipeline.sln` with empty project shells: `Shared.Contracts`, `Ingest.Api`,
      `Search.Api`, `RedisIndexer.Worker`, `ElasticIndexer.Worker`.
- [ ] Add a .NET `.gitignore`.
- [ ] Add a top-level README describing how to run what exists so far (kept in sync per phase).

## Phase 1 — Ingest.Api + MongoDB

- [ ] Add a MongoDB client to `Ingest.Api`.
- [ ] Implement `GET /ingest?text=...`, storing `{ id, text, createdAt }` in MongoDB.
- [ ] Add a minimal `docker-compose.yml` with just `mongo` + `ingest-api`.
- **Verify:** hit `http://localhost:<port>/ingest?text=hello` in a browser, confirm the document
  appears in Mongo (e.g. via `mongosh` or Compass).

## Phase 2 — RabbitMQ publish

- [ ] Define the `TextSubmitted` message (Id, Text, CreatedAt) in `Shared.Contracts`.
- [ ] Add a RabbitMQ client to `Ingest.Api`; after a successful Mongo write, publish `TextSubmitted`
      to a fanout/topic exchange.
- [ ] Add `rabbitmq` to `docker-compose.yml` and wire `ingest-api` to it.
- **Verify:** hit `/ingest?text=hello`, confirm the message is visible in the RabbitMQ management UI.

## Phase 3 — Redis consumer

- [ ] Scaffold `RedisIndexer.Worker` as a BackgroundService, binding its own queue to the exchange.
- [ ] On message received, write the text to Redis.
- [ ] Add `redis` and `redis-indexer` to `docker-compose.yml`.
- **Verify:** hit `/ingest?text=hello`, confirm the value appears in Redis (e.g. via `redis-cli`).

## Phase 4 — Elasticsearch consumer

- [ ] Scaffold `ElasticIndexer.Worker` as a BackgroundService, binding its own queue to the same
      exchange.
- [ ] On message received, index the document into Elasticsearch.
- [ ] Add `elasticsearch` and `elastic-indexer` to `docker-compose.yml`.
- **Verify:** hit `/ingest?text=hello`, confirm the document is indexed (e.g. via Elasticsearch's
  `_search` endpoint).

## Phase 5 — Search.Api

- [ ] Scaffold `Search.Api`.
- [ ] `GET /search/mongo?q=...` — text search against MongoDB.
- [ ] `GET /search/redis?q=...` — lookup/search against Redis.
- [ ] `GET /search/elastic?q=...` — full-text search against Elasticsearch.
- [ ] Add `search-api` to `docker-compose.yml`.
- **Verify:** ingest a few values, then hit all three search endpoints from a browser and compare
  results/behavior across stores.

## Phase 6 — Docker Compose hardening

- [ ] Add healthchecks and `depends_on` conditions so services start in the right order.
- [ ] Confirm the full end-to-end flow works via `docker compose up` from a clean state.

## Phase 7 — Kubernetes (local)

- [ ] Decide on a local cluster tool (minikube vs kind) with the user.
- [ ] Write k8s manifests (Deployments + Services) for infra and app services, or decide whether to
      use Helm charts for the infra pieces.
- [ ] Verify the same end-to-end flow works against the local cluster.
