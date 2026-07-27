# Worker Processing

## Queue topology

Initial vertical slice uses `repository.pipeline` plus `godforge.dead-letter`. As throughput grows it splits into `repository.sync`, `repository.parse`, `repository.health`, `repository.context`, `repository.ai-analysis` and `repository.finalize` without changing Domain/Application contracts.

## Message envelope

Required: `schemaVersion`, `messageId`, `jobId`, `projectId`, optional `repositoryId`, `correlationId`, `createdAt`, `attemptCount`, `inputHash`.

## Reliability

- Durable queue and persistent message.
- PostgreSQL job state updated before/after work.
- One repository mutation at a time through Redis lock before multi-worker production rollout.
- Bounded exponential retry for transport/I/O/provider failures.
- Invalid schema, missing identifiers and exhausted retries go to DLQ.
- Completion is published only after durable output commits.
- Cancellation is cooperative and checked between stages.
- Same repository/commit/config/input hash must not duplicate snapshots or AI runs.
