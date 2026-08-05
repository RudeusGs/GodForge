# Setup Checklist

## Local

- [ ] Copy `.env.example` to `.env` and replace defaults where required.
- [ ] Start PostgreSQL, Redis, RabbitMQ and MinIO.
- [ ] Start Forgejo profile only when hosted Git is tested.
- [ ] Restore backend and frontend dependencies.
- [ ] Apply migrations or run the documented local initialization path.
- [ ] Run unit and integration tests.
- [ ] Verify health endpoints.

## Production preparation

- [ ] Replace every example secret.
- [ ] Configure TLS and trusted proxy behavior.
- [ ] Configure object buckets and lifecycle rules.
- [ ] Configure Forgejo service account, webhook secret and backups.
- [ ] Configure outbox dispatcher and DLQ monitoring.
- [ ] Configure OpenTelemetry, dashboards and alerts.
- [ ] Verify PostgreSQL, MinIO and Forgejo backup/restore.
- [ ] Run security and load test plans.
- [ ] Confirm retention and deletion policies.
