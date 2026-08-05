# 12. Worker Processing

## Job types

- repository provision/reconcile.
- repository sync/checkout.
- Godot validation.
- parser.
- graph/health analysis.
- incremental comparison.
- AI advisory.
- asset validation/preview/purge.
- report export.
- notification/email dispatch.
- search indexing and retention.

## Message envelope

```json
{
  "schemaVersion": 1,
  "messageId": "uuid",
  "jobId": "uuid",
  "organizationId": "uuid",
  "projectId": "uuid",
  "repositoryId": "uuid|null",
  "correlationId": "string",
  "createdAt": "utc",
  "attemptCount": 0,
  "inputHash": "sha256",
  "payloadRef": "small identifiers only"
}
```

Messages never contain credentials, raw source, binary assets or large prompts.

## Durable creation

1. Authorize and validate.
2. Compute idempotency/input identity.
3. Create or return equivalent active job.
4. Write business state and outbox event in one transaction.
5. Dispatcher publishes persistent message.
6. Return 202 and job DTO.

## Consumer algorithm

1. Start structured logging scope.
2. Validate schema/IDs and load durable job.
3. Deduplicate via inbox/message identity.
4. Ignore safely if terminal/cancelled.
5. Revalidate project/repository state.
6. Acquire repository lock when mutable workspace/provider mutation is involved.
7. Mark running and heartbeat.
8. Execute bounded stage with cancellation/timeout.
9. Upsert output under deterministic identity.
10. Commit output and terminal state.
11. Emit outbox completion/activity/notification.
12. Release owner-token lock and clean temporary resources.

## States

`queued -> running -> completed`

Alternative transitions: `running -> retrying -> queued`, `queued/running/retrying -> cancelled`, `running -> timeout`, `queued/running/retrying -> failed/dead_lettered`.

Progress is monotonic 0-100. Heartbeat is independent of progress.

## Retry policy

- Transient infrastructure/provider errors: bounded exponential backoff with jitter.
- Invalid credentials, semantic invalid input and authorization/state conflicts: no automatic retry.
- Poison schema or impossible identity: DLQ.
- Data-level parser diagnostics may complete job with warnings instead of failing entire pipeline.

## Locks

Redis key includes repository ID. Value is random owner token. Release verifies token; TTL is renewed for long jobs. Lock failure usually retries. Immutable snapshot-only processing minimizes lock duration.

## Workspace

- One isolated directory per job/attempt.
- Non-root process, configured root, quotas and no Docker socket.
- No untrusted code execution.
- Cleanup on success, failure, cancellation and timeout; failed cleanup is observable.

## Queue evolution

Initial deployment may use a pipeline queue. Logical contracts must allow split queues for repository, parser, analysis, AI, asset and report workloads.
