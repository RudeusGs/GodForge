# Operations Runbook

## M2 hosted Git recovery

- **Forgejo outage:** Restore the configured internal endpoint and non-admin
  service token. Membership changes remain authoritative immediately; monitor
  access generations until `accessConfirmedGeneration == accessGeneration` with
  no access error. Do not report a completed external revocation while pending.
- **Interrupted Worker/provisioning:** Restart the Worker. Three-minute job
  leases and recovery outbox records permit reclaim after heartbeat expiry.
  Provider ownership markers recover uncertain creation outcomes. For a terminal
  provisioning failure, an authorized manager repeats the original hosted POST;
  it reuses the durable operation and provider identity.
- **Reconciliation failure/drift:** Inspect sanitized job error codes, provider
  availability and account bindings. The five-minute scan schedules up to 100
  eligible repositories, including failed operations; active jobs retain their
  existing backoff. Verify provider read-back, never force a database success flag.
- **Missing/stale webhook:** Correct the server callback/secret and Forgejo
  outbound destination allowlist. Provisioning/reconciliation idempotently
  installs or repairs the managed hook and verifies its non-secret configuration.
  Confirm a real subsequent push reaches the callback and revision sync completes.
- **Outbox exhaustion:** Inspect failed/dead-letter jobs and outbox records,
  restore RabbitMQ/connectivity first, and use supported operation retry paths.
  Never delete pending deliveries or acknowledge unsuccessful reconciliation.

Monitor the oldest unprocessed outbox `created_at` (`pending`, `processing`,
`failed` statuses), queued/running/retrying/dead-letter job counts by queue,
attempt counts/availability, repository access generation lag and access error
codes, webhook records and failed synchronization jobs. Logs expose stable failure codes and
IDs. Do not dump provider headers, personal tokens, hook secrets or raw payloads.

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
