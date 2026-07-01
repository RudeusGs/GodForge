# 12. Worker Processing

## Purpose

Worker Processing defines the queues, job lifecycle, retries, DLQ (Dead-Letter Queue), timeouts, idempotency, progress tracking, cancellation, repository locks, and metrics for asynchronous tasks in GodForge.

## Queue Types

| Queue | Producer | Consumer | Main Message |
| --- | --- | --- | --- |
| `repository.clone` | Repository Service | Clone/Git Worker | `RepositoryCloneRequested` |
| `repository.sync` | Repository/Git Service | Git Worker | `RepositorySyncRequested` |
| `metadata.parse` | Project Service, Git Worker | Parser Worker | `ParseRequested` |
| `metadata.analyze` | Project Service, Parser Worker | Analyze Worker | `AnalyzeRequested` |
| `diff.scene` | Diff Service | Diff Worker | `DiffRequested` |
| `preview.asset` | Metadata Service | Preview Worker | `PreviewRequested` |
| `notification.dispatch` | Domain services/workers | Notification Worker (if separate) | `NotificationDispatchRequested` |

## Minimum Message Contract

Each message must contain:

- `schemaVersion`
- `messageId`
- `jobId`
- `correlationId`
- `projectId`
- `repositoryId` (if applicable)
- `actorId` (if user-triggered)
- `createdAt`
- `attemptCount`
- `inputHash`
- payload per job type

## Job Lifecycle

Canonical job status values: `queued`, `running`, `retrying`, `completed`, `failed`, `cancelled`, `timeout`, `dead_lettered`.

| State | Meaning | Valid Transitions |
| --- | --- | --- |
| `queued` | Job created and published to queue. | `running`, `cancelled`, `failed` |
| `running` | Worker is processing the job. | `completed`, `failed`, `retrying`, `cancelled`, `timeout` |
| `retrying` | Job encountered a transient error and is awaiting retry. | `queued`, `failed`, `dead_lettered` |
| `completed` | Job succeeded. | Terminal |
| `failed` | Job failed without retry or exhausted retries. | Terminal or manual retry if policy allows |
| `cancelled` | Job was cancelled before or during safe processing. | Terminal |
| `timeout` | Job exceeded configured timeout and worker stopped/rolled back safely. | Terminal or manual retry if policy allows |
| `dead_lettered` | Message moved to DLQ due to poison message, schema error, or retry exhaustion needing inspection. | Manual inspect/requeue |

## Retry Policy

| Error Type | Retry | Notes |
| --- | --- | --- |
| Transient network error during clone/fetch | Yes | Exponential backoff, limited attempts. |
| DB/MinIO/RabbitMQ transient error | Yes | Retry if operation is idempotent. |
| Invalid credential | No | Requires user to update the credential. |
| Invalid repository URL | No | Validation error. |
| Godot file syntax error | No at job-level | Log warning at file-level, continue if in partial mode. |
| Message payload schema error | No | Move to DLQ. |
| Repeated timeout | Limited | Mark as `timeout`; after limit move to `dead_lettered` or `failed` based on queue policy. |

## Dead-Letter Queue (DLQ)

- Every main queue must have a corresponding DLQ.
- DLQ records/messages must retain `messageId`, `jobId`, `correlationId`, error code, attempt count, and sanitized payload.
- When a message enters the DLQ, the associated job must transition to `dead_lettered` if no automatic retries remain.
- Must not contain plaintext credentials.
- Operations can manually inspect and requeue messages after fixing the root cause.

## Timeouts

| Job Type | Recommended Timeout |
| --- | --- |
| Clone repo < 500MB | 5 minutes, scaling with repo size config. |
| Fetch/sync | 2 minutes, depending on remote. |
| Parse | Based on file count; default 10 mins for medium project. |
| Analyze | 5 minutes for medium project. |
| Scene diff | 30 seconds to 2 minutes depending on scene size. |
| Preview | Short timeout, 30-60 seconds. |

## Idempotency

| Worker | Idempotency Key | Policy |
| --- | --- | --- |
| Clone Worker | repository id + remote URL + branch + input hash | If workspace is already at the correct commit/state, do not clone again. |
| Parser Worker | project id + repository id + commit hash + file hash | Upsert metadata; do not duplicate scenes/nodes/assets. |
| Analyze Worker | project id + metadata version + rule version | Do not write report if a newer metadata version is already active. |
| Diff Worker | project id + scene path + base revision + target revision | Cache hit returns the old result if still valid. |
| Preview Worker | metadata version + entity hash | Do not create duplicate artifacts. |

## Progress Updates

- `jobs.progress` is a number from 0-100.
- Workers update progress in batches to avoid spamming the DB.
- SignalR sends `JobProgressUpdate` to the project room.
- The job detail endpoint is the source of truth if SignalR disconnects.

## Cancellation

- Users with `developer+` can cancel jobs if the job type supports it.
- Canceling clone/Git operations is only executed if it can stop safely.
- Workers must check cancellation tokens between batches.
- A cancelled job must not leave a repository lock hanging.

## Repository Locks

- Lock key: `lock:repo:{repository_id}`.
- Default TTL: 5 minutes or per job type.
- Operations requiring locks: clone/fetch, commit, push, pull, merge, checkout, diff (if checkout requires a mutable workspace).
- Lock owner/correlation ID is saved for debugging.
- If lock acquisition fails, the API returns 423 or the job retries depending on the type.

## Error Handling

- Async errors must be saved to `jobs.error_code` and `jobs.error_message` (sanitized).
- Workers log detailed technical information with the correlation ID, but do not log secrets.
- User-facing errors must be concise and suggest next actions.
- Partial parsing is allowed if file-level errors do not break metadata consistency.

## Worker Metrics

| Metric | Description |
| --- | --- |
| `job_duration_seconds` | Job processing time by type/status. |
| `job_queue_depth` | Number of waiting messages per queue. |
| `job_retry_total` | Number of retries by type/error. |
| `job_dead_letter_total` | Number of messages sent to DLQ. |
| `job_progress_update_total` | Number of progress updates. |
| `repository_lock_wait_seconds` | Time spent waiting for locks. |
| `repository_lock_timeout_total` | Number of lock timeouts/failures. |
| `worker_active_jobs` | Number of currently running jobs per worker. |

## Deployment Model for MVP

- This document refers to **logical workers** (Clone Worker, Parser Worker, Analyze Worker, etc.).
- During the MVP phase, all these logical workers may be deployed together within a single shared host project, `GodForge.Worker`, to simplify operations.
- However, each queue **must still map to an independent consumer/handler** within the codebase, even if they run in the same process.
- The internal architecture must ensure that in the future, each consumer can be easily split into its own separate worker service without altering message contracts or rewriting business logic.
- Default retry configuration: 3 attempts with exponential backoff for transient errors; individual queues may override this via configuration.
