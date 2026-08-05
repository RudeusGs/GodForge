# Durable Jobs

## Purpose

Expose safe, project-scoped status and control for long-running work.

## Actors

Authorized project members, Worker, SystemAdmin operations.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-19.1 | Create durable jobs before publish | Must |
| FR-19.2 | Read list/detail/progress/history | Must |
| FR-19.3 | Cooperative cancellation and controlled retry | Must |
| FR-19.4 | Retry, timeout, heartbeat and DLQ visibility | Must |

## Main flow

1. Application authorizes and validates requested work.
2. Transaction creates job and outbox event.
3. Dispatcher publishes message.
4. Worker updates running/progress/heartbeat/attempts.
5. Terminal state and outputs commit before completion notification.

## Error and edge cases

- Publish delay/outbox backlog.
- Duplicate delivery.
- Stale heartbeat, timeout or cancellation.
- Poison message and retry exhaustion.
- Project archived/deleted while queued.

## Authorization and security

- Job reads are project-scoped.
- Client cannot choose privileged job type or arbitrary input reference.
- Safe errors only; raw provider output is restricted.

## Async processing and idempotency

- No module-specific durable job is created by read operations.
- Writes, where present, use normal transaction/concurrency rules and do not perform hidden heavy work.

## Acceptance criteria

- `AC-FR-19-01`: Duplicate message does not duplicate output.
- `AC-FR-19-02`: Cancellation releases locks and cleans workspace.
- `AC-FR-19-03`: Job state survives API/worker restart.
- `AC-FR-19-04`: DLQ item can be inspected/requeued through audited operations.

## Related API

- `GET /projects/{projectId}/jobs`, detail, cancel, retry; admin DLQ endpoints

## Related data

- `ops.jobs`, `ops.job_attempts`, `ops.job_events`, `ops.job_cancellations`, `ops.outbox_messages`, `ops.inbox_messages`, `ops.dead_letter_messages`

## Tests and observability

- Test suite: `TC-JOB-*`, including duplicate delivery, retry, DLQ, cancellation, timeout, stale heartbeat and restart.
- Metrics: queue lag, job duration/state, attempts, heartbeat age, cleanup failure and DLQ count.
- Traces correlate API request, outbox, message and worker attempt.
