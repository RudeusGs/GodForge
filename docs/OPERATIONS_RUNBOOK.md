# Operations Runbook

## Daily checks

- API and Worker readiness.
- PostgreSQL connection and storage growth.
- RabbitMQ queue lag, unacked count and DLQ.
- Outbox age and failed dispatch count.
- Redis memory and stale lock count.
- MinIO capacity and failed operations.
- Forgejo health and webhook failures.
- AI provider error/latency and token budget.

## Common incidents

### Queue backlog

1. Confirm dependency health and worker concurrency.
2. Identify dominant job type and oldest message.
3. Do not purge queue without reconciling PostgreSQL jobs.
4. Scale workers or pause producers according to rate-limit policy.
5. Record incident and affected job IDs.

### Stale repository lock

1. Verify owner token and related job heartbeat.
2. Confirm worker is not active.
3. Use administrative unlock procedure that records an audit event.
4. Clean workspace only after job state is reconciled.

### AI outage

- Keep deterministic pipeline active.
- Mark AI stage degraded.
- Stop automatic retries after configured budget.
- Notify users that health results remain valid.

### Forgejo outage

- Stop hosted-repository mutation workflows.
- Continue reads from already persisted GodForge metadata where safe.
- Do not claim push/permission changes succeeded.
- Reconcile webhooks and permissions after recovery.

### Object storage outage

- Block new asset/report publication.
- Avoid issuing signed URLs.
- Preserve pending jobs for controlled retry.

## Administrative rules

- All manual state repair uses audited commands/scripts.
- Never edit production rows ad hoc without a written incident record and peer review.
- Never expose raw provider payloads or credentials in incident tickets.
