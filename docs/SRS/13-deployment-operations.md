# 13. Deployment & Operations

## Local Dev Setup

Local development requires at minimum:

- Vue 3 Frontend.
- API Backend.
- PostgreSQL.
- Redis.
- RabbitMQ.
- MinIO.
- Worker process.

Local workflow:

1. Configure environment variables.
2. Start infrastructure using Docker Compose or equivalent local services.
3. Run database migrations.
4. Start the API.
5. Start the worker.
6. Start the frontend.
7. Seed System Admin/dev data if needed.

## Docker Compose Setup

Docker Compose for dev/staging should include these services:

| Service | Role |
| --- | --- |
| `godforge-api` | REST API and SignalR. |
| `godforge-worker` | Parser/Analyzer/Diff/Preview/Git workers or shared worker host. |
| `godforge-frontend` | Vue frontend if deployed containerized. |
| `postgres` | Database. |
| `redis` | Cache/rate limit/repository lock. |
| `rabbitmq` | Async queues and DLQ. |
| `minio` | Object storage. |
| `prometheus` | Metrics scraping. |
| `grafana` | Monitoring dashboard. |
| `loki` | Log aggregation if enabled. |

Internal services should not expose public ports beyond development needs.

## Production Deployment Overview

- API and frontend placed behind a reverse proxy/load balancer.
- HTTPS is mandatory.
- API is stateless for horizontal scaling.
- Workers scale based on queue depth.
- PostgreSQL, Redis, RabbitMQ, and MinIO run in a private network.
- Secrets are provisioned via a secret manager or secure environment injection.
- Migrations are run in a controlled manner before deploying new versions.

## Environment Variables

| Variable | Purpose | Secret |
| --- | --- | :---: |
| `GODFORGE_ENVIRONMENT` | dev/staging/prod. | No |
| `GODFORGE_API_BASE_URL` | Public API URL. | No |
| `GODFORGE_DATABASE_URL` | PostgreSQL connection string. | Yes |
| `GODFORGE_REDIS_URL` | Redis connection string. | Yes |
| `GODFORGE_RABBITMQ_URL` | RabbitMQ connection string. | Yes |
| `GODFORGE_MINIO_ENDPOINT` | MinIO endpoint. | No |
| `GODFORGE_MINIO_ACCESS_KEY` | MinIO access key. | Yes |
| `GODFORGE_MINIO_SECRET_KEY` | MinIO secret key. | Yes |
| `GODFORGE_JWT_SIGNING_KEY` | JWT signing key if using symmetric signing. | Yes |
| `GODFORGE_ENCRYPTION_KEY` | Key for encrypting Git credentials. | Yes |
| `GODFORGE_CORS_ORIGINS` | CORS allowlist. | No |
| `GODFORGE_SMTP_*` | Email notification/invites if enabled. | Yes |

## Secret Management

- Do not commit production `.env` files.
- Production secrets must be rotatable.
- Logs must not output connection strings or secrets.
- Git credentials in the DB must be encrypted even if the DB is private.

## Database Migrations

- Migrations must be code-reviewed alongside code changes.
- Backup the database before major production migrations.
- Destructive migrations require a rollback plan or safe data migration strategy.
- App version and migration version must be compatible.

## Backup / Restore

| Data | Backup Strategy | Restore Priority |
| --- | --- | --- |
| PostgreSQL Core | Full + incremental/WAL if available | Highest |
| PostgreSQL Metadata | Periodic or regenerate | Medium |
| MinIO artifacts | Bucket backup per retention | Medium |
| RabbitMQ config/DLQ | Durable queues + config backup | Medium |
| Redis cache/lock | Not required | Low |

Restore drills must confirm:

- API can read core data.
- Membership/RBAC functions correctly.
- Repository configs retain valid credential references.
- Metadata or regeneration pipelines operate successfully.
- MinIO artifacts remain readable if within the retention period.

## Monitoring

Prometheus scraping:

- API metrics.
- Worker metrics.
- RabbitMQ queue depths.
- PostgreSQL health/pool usage.
- Redis availability.
- MinIO health.

Minimum Grafana dashboards:

- API latency/error rate.
- Worker success/failure/retry/DLQ stats.
- Queue depth per queue.
- DB/Redis/RabbitMQ/MinIO health.
- Repository lock contention.

## Logging

- Structured JSON logs.
- Correlation IDs across API/workers.
- Log aggregation via Loki or equivalent stack.
- Log retention of 30-90 days depending on the environment.
- Do not log secrets, tokens, passwords, PATs, or sensitive server paths.

## Alerting

| Alert | Suggestion Condition |
| --- | --- |
| API high error rate | 5xx > 1% over 5 minutes. |
| API latency high | P95 exceeds NFR for 10 minutes. |
| Queue backlog | Queue depth exceeds threshold based on worker pool. |
| Worker failures | Unusual spike in failures/retries. |
| DLQ not empty | New message appears in DLQ. |
| DB unavailable | Health check fails. |
| Redis/RabbitMQ/MinIO unavailable | Health check fails. |
| Storage pressure | Disk/object storage utilization exceeds threshold. |

## Incident Handling

1. Acknowledge alert, timestamp, and correlation ID if available.
2. Determine blast radius: API, worker, DB, queue, storage, or Git provider.
3. Check dashboard metrics and logs.
4. If queue-related, inspect job states and DLQ.
5. Restore service according to runbooks.
6. Write post-incident notes: root cause, impact, preventative measures.

## Data Retention

| Data Type | Recommended Retention |
| --- | --- |
| Project soft delete | 30 days before hard-delete if policy enabled. |
| Notification | 90 days. |
| Activity/Audit Log | 1 year or according to policy. |
| Application logs | 30-90 days. |
| Diff artifacts | 7 days or per cache policy. |
| Reports export | 30 days. |
| Redis cache | Minutes/hours TTL based on key. |

## Disaster Recovery

| Incident | Impact | Recovery |
| --- | --- | --- |
| API container crash | Users cannot access the API. | Restart/scale API, check health endpoints and dependencies. |
| Worker crash | Job backlog grows. | Restart worker, inspect queues/DLQ, safely requeue jobs. |
| PostgreSQL failure | Loss of read/write for core data. | Restore backup, run migrations if needed, verify integrity. |
| RabbitMQ failure | Cannot create/process new jobs. | Recover broker, queues, DLQ, and resume workers. |
| MinIO failure | Cannot read/write artifacts. | Restore buckets or regenerate artifacts if possible. |
| Redis failure | Temporary loss of cache/locks/rate limits. | Restart Redis; rebuild cache; verify stale locks. |

Core data must be prioritized for recovery over metadata/artifacts since metadata can often be regenerated from repositories.

## MVP Decisions

- Dev/staging use user secrets or environment variables; production uses a secret manager or secure environment injection from the deployment platform.
- Default retentions use the Data Retention table above; production may override via configuration but must not be shorter than approved audit requirements.
