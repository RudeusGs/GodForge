# 13. Deployment and Operations

## Environments

- Local: Docker Compose dependencies, API/Worker/Frontend run locally.
- Test/CI: ephemeral PostgreSQL and provider substitutes/containers.
- Staging: production-like topology and sanitized datasets.
- Production: managed or hardened services, protected secrets, TLS, monitoring and backups.

## Services

- Vue static frontend behind HTTPS reverse proxy/CDN.
- ASP.NET Core API with readiness/liveness.
- One or more Worker instances with job-type concurrency limits.
- PostgreSQL, Redis, RabbitMQ, MinIO and Forgejo.
- Optional OTLP collector, Prometheus/Grafana and centralized logs.

## Production requirements

- No example/default password or token.
- TLS for external traffic and secure internal transport where environment supports it.
- Trusted proxy/header configuration.
- Database migrations run as controlled release step, not uncontrolled concurrent startup.
- Forgejo registration disabled; provisioning through GodForge service account.
- MinIO buckets private by default with lifecycle and versioning policy.
- RabbitMQ durable queues, DLQ and access isolation.
- Worker non-root, read-only base filesystem where possible, constrained CPU/memory/disk.

## Scaling

- API scales horizontally; no local business state.
- Worker scales by queue/job type after lock/idempotency controls.
- PostgreSQL read/query tuning precedes premature cache expansion.
- Graph and report payloads move to artifacts when too large.

## Release and rollback

- Use immutable artifacts and release version.
- Back up before schema change.
- Prefer forward fix for migrated database; application rollback only when schema-compatible.
- Validate health, queue/outbox lag, error rate and critical workflow after release.

## Recovery

Follow `../BACKUP_RESTORE_RUNBOOK.md`, `../OPERATIONS_RUNBOOK.md` and `../INCIDENT_RESPONSE.md`.
