# 2. Architecture

## 2.1 Components

| Component | Responsibility |
|---|---|
| Vue Web UI | User workflows, visualization and job progress presentation. |
| ASP.NET Core API | Authentication, authorization, synchronous CRUD/read models and durable job creation. |
| PostgreSQL | Durable business state, metadata, jobs, reports, audit and object references. |
| Redis | Cache, rate-limit support and owner-token distributed locks. |
| RabbitMQ | Durable message transport and DLQ. |
| Worker | Git workspace, validation, parser, analysis, AI, asset and report jobs. |
| Forgejo | Hosted Git repositories, refs, clone/push/pull and provider webhooks. |
| MinIO | Protected assets, previews, large reports and generated artifacts. |
| Gemini | Optional advisory analysis only. |

## 2.2 Clean Architecture

```text
Domain <- Application <- Infrastructure
                  ^          ^
                  |          |
                 API       Worker
```

API and Worker depend on Application and provider registrations. Business rules and permissions remain in Domain/Application.

## 2.3 Data ownership

- Forgejo/external provider owns Git objects and refs.
- PostgreSQL owns GodForge state and versioned analysis records.
- MinIO owns object bytes; PostgreSQL owns object metadata and authorization references.
- RabbitMQ does not own job state.
- Redis data may be lost without losing business truth.

## 2.4 Repository modes

- `linked`: external HTTPS repository with encrypted credential reference where needed.
- `hosted`: Forgejo repository provisioned by GodForge.

Both modes produce the same immutable revision/analysis model.

## 2.5 Analysis pipeline

```text
resolve revision
-> secure checkout
-> validate Godot project
-> inventory/hash
-> deterministic parse
-> graph and health rules
-> incremental impact when eligible
-> bounded/redacted context
-> optional Gemini advisory
-> atomic persist/artifact publication
-> activity/notification/metrics
```

## 2.6 Consistency

- Business changes and outbox records are committed atomically.
- Consumers deduplicate messages and upsert deterministic outputs.
- Hosted Git permission changes are eventually consistent and reconciled.
- An analysis run is published only after required outputs commit.

## 2.7 Scalability path

The initial single Worker host contains logical consumers. Queues and handlers remain separable so repository, parser, analysis, AI and asset workers can later scale independently.

## 2.8 Failure modes

- Gemini unavailable: deterministic result completes with AI degraded status.
- Forgejo unavailable: hosted Git mutations pause; persisted analysis remains readable.
- RabbitMQ unavailable: job/outbox remains durable and dispatch resumes.
- MinIO unavailable: asset/report publication waits or fails safely; metadata does not claim success.
- Worker crash: heartbeat expires, job becomes retryable/timeout according to policy.
