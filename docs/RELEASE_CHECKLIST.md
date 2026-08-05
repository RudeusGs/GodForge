# Production Release Checklist

- [ ] Tagged immutable build artifacts.
- [ ] Database migration backup completed.
- [ ] Migration tested against production-like copy.
- [ ] Secrets loaded from protected source and rotation owners assigned.
- [ ] TLS, CORS, proxy headers and rate limits verified.
- [ ] Forgejo webhook and permission reconciliation verified.
- [ ] RabbitMQ queues, DLQ and outbox lag dashboards healthy.
- [ ] MinIO bucket policy and lifecycle verified.
- [ ] PostgreSQL, Forgejo and MinIO restore drill passed.
- [ ] Security regression suite passed.
- [ ] Load test meets release budget.
- [ ] Rollback/forward-fix decision and owner documented.
- [ ] Incident channel and on-call contacts ready.
- [ ] User-facing release notes prepared.
