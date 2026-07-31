# Plan.md

Step-by-step build order for PolyglotPipeline. Each phase is meant to be one (or a few) git commits
the user makes and reviews themselves before moving on. Do not jump ahead to a later phase or
implement multiple phases in one pass unless explicitly asked. Check items off as they land.

## Phase 0 — Scaffolding

- [x] Create `PolyglotPipeline.sln` with empty project shells: `Shared.Contracts`, `Ingest.Api`,
      `Search.Api`, `RedisIndexer.Worker`, `ElasticIndexer.Worker`.
- [x] Add a .NET `.gitignore`.
- [x] Add a top-level README describing how to run what exists so far (kept in sync per phase).

## Phase 1 — Ingest.Api + MongoDB

- [x] Add a MongoDB client to `Ingest.Api`.
- [x] Implement `GET /ingest?text=...`, storing `{ id, text, createdAt }` in MongoDB.
- [x] Add a minimal `docker-compose.yml` with `mongo`, `mongo-express` (basic auth disabled, so it's
      reachable straight from a browser like everything else here), and `ingest-api`.
- **Verify:** hit `http://localhost:<port>/ingest?text=hello` in a browser, confirm the document
  appears in Mongo via the `mongo-express` UI (or `mongosh`/Compass).

## Phase 2 — Messaging publish (RabbitMQ + Kafka)

- [x] Define the `TextSubmitted` message (Id, Text, CreatedAt) in `Shared.Contracts`.
- [x] Add a `broker` query parameter to `GET /ingest` (`rabbitmq` or `kafka`) that selects which
      broker this request's message goes to; reject anything else with a plain 400.
- [x] Add a RabbitMQ client to `Ingest.Api`; after a successful Mongo write, publish `TextSubmitted`
      to a fanout/topic exchange when `broker=rabbitmq`.
- [x] Add a Kafka client to `Ingest.Api`; publish `TextSubmitted` to a topic when `broker=kafka`.
- [x] Add `rabbitmq` (the `-management` image variant, so its browser-based management UI is
      available for free) and `kafka` (KRaft mode, no ZooKeeper) to `docker-compose.yml` and wire
      `ingest-api` to both.
- [x] Add `kafka-ui` (basic auth disabled) to `docker-compose.yml` for browser-based topic
      inspection — Kafka has no built-in UI the way RabbitMQ does.
- **Verify:** hit `/ingest?text=hello&broker=rabbitmq`, confirm the message is visible in the
  RabbitMQ management UI; hit `/ingest?text=hello&broker=kafka`, confirm the message via the
  `kafka-ui` UI (or a console consumer).

## Phase 3 — Redis consumer

- [x] Scaffold `RedisIndexer.Worker` as a BackgroundService running two independent listeners: one
      binding a queue to the RabbitMQ exchange, one subscribed to the Kafka topic.
- [ ] Both listeners call the same handler on message received, which writes the text to Redis —
      the handler must not care which broker delivered the message.
- [ ] Add `redis` and `redis-indexer` to `docker-compose.yml`.
- [ ] Add `redis-commander` (basic auth disabled) to `docker-compose.yml` for browser-based key
      inspection — Redis has no built-in UI.
- **Verify:** hit `/ingest?text=hello&broker=rabbitmq` and separately `broker=kafka`; confirm both
  values appear in Redis via the `redis-commander` UI (or `redis-cli`).

## Phase 4 — Elasticsearch consumer

- [ ] Scaffold `ElasticIndexer.Worker` as a BackgroundService with the same dual-listener shape as
      `RedisIndexer.Worker` (its own RabbitMQ queue + Kafka topic/consumer group).
- [ ] Both listeners call the same handler on message received, which indexes the document into
      Elasticsearch.
- [ ] Add `elasticsearch` and `elastic-indexer` to `docker-compose.yml`.
- **Verify:** hit `/ingest?text=hello&broker=rabbitmq` and separately `broker=kafka`; confirm the
  document is indexed for both (e.g. via Elasticsearch's `_search` endpoint).

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
