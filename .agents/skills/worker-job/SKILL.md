---
name: worker-job
description: skill for implementing async background jobs and worker processing in the godforge backend. use when adding or changing rabbitmq producers/consumers, clone/fetch/parse/analyze/diff/preview jobs, job lifecycle handling, retry/dlq behavior, progress reporting, idempotency, distributed repository locks, observability, and worker regression tests.
---

# Worker Job

Use this skill when implementing or modifying GodForge background jobs for long-running work such as repository clone/fetch, project parse, project analyze, scene diff, and preview generation.

GodForge API requests must stay lightweight. Any operation that can exceed the API latency target or depends on repository/project size must create a durable job, publish a RabbitMQ message, and return a `202 Accepted` response with a `jobId` and `correlationId`.

## Required SRS Sources

Before coding, read the relevant sections in `docs/SRS/`:

- `02-architecture.md` or architecture overview: async processing, API/worker boundaries, observability.
- `05-api.md`: job endpoints and API response conventions.
- `06-security.md`: RBAC rules for who can trigger/cancel/retry jobs.
- `09-database.md` or database schema: `jobs`, metadata, artifacts, activities, notifications.
- `11-job-queue.md` or job queue specification: job types, retry and idempotency rules.
- `12-worker-processing.md`: producer/consumer contracts, timeout, DLQ, idempotency.
- Relevant FR/WF for the feature: repository clone, analyze, parser, diff, preview, dashboard, activity log.

Confirm these items before writing code:

- Producer service and API endpoint.
- Worker/consumer name and queue.
- Required RBAC permission.
- Job type and job state transitions.
- Message schema version and required fields.
- Idempotency key or input hash.
- Timeout and retry classification.
- Whether a Redis repository lock is required.
- Metadata/artifact output target: PostgreSQL, MinIO, or both.
- Activity log, notification, cache invalidation, and SignalR progress events.

## Canonical Architecture

```text
API Controller
  -> Application service validates permission, state, and input
  -> Create durable job row in PostgreSQL
  -> Publish RabbitMQ message after DB commit / outbox publish
  -> Return 202 Accepted { data: { jobId, status }, correlationId }

RabbitMQ durable queue
  -> Worker consumes message
  -> Validate message schema, freshness, and idempotency
  -> Acquire lock if needed
  -> Mark job running and heartbeat
  -> Execute engine/service work
  -> Persist metadata/artifact/output
  -> Update progress and emit SignalR events
  -> Complete/fail/retry/dead-letter with activity log and notification
```

Do not put business rules directly in RabbitMQ plumbing or API controllers. API/Application decides whether the job may be created. Worker executes the already-authorized job safely and revalidates stale state before committing results.

## Job Types

Use stable job type names in code and persistence:

| Job Type | Producer | Consumer | Output | Lock Required |
| --- | --- | --- | --- | --- |
| `clone` | Repository Service | Clone Worker / Git Worker | repository workspace, repository state, initial parse/analyze request | Yes: `repo-lock:{repositoryId}` |
| `fetch` | Repository/Git Service | Git Worker | updated refs, optional analyze request | Yes |
| `parse` | Project Service / Analyze Orchestrator | Parser Worker | scene/resource/script metadata | Usually yes for working-tree reads; snapshot-only parse may not require lock |
| `analyze` | Project Service / Scheduler | Analyze Worker | dependency graph, statistics, health report | Lock if reading mutable workspace |
| `diff` | Diff API / Diff Service | Diff Worker | diff result in PostgreSQL or MinIO | Lock only if comparing working tree |
| `preview` | Project Service / Metadata Service | Preview Worker | preview/snapshot artifact | Depends on source |

Prefer immutable inputs such as `commitHash`, `snapshotRef`, or `metadataVersion`. Avoid workers reading a mutable working tree without a lock.

## Message Contract

Every job message must include enough data for retries, delayed delivery, stale-message detection, and tracing.

```csharp
namespace GodForge.Application.Features.Jobs.Messages;

public interface IJobMessage
{
    string SchemaVersion { get; }
    Guid MessageId { get; }
    Guid JobId { get; }
    string JobType { get; }
    Guid ProjectId { get; }
    Guid? RepositoryId { get; }
    Guid? ActorId { get; }
    string CorrelationId { get; }
    DateTimeOffset CreatedAt { get; }
    int AttemptCount { get; }
    string InputHash { get; }
}

public sealed record ParseProjectJobMessage(
    string SchemaVersion,
    Guid MessageId,
    Guid JobId,
    string JobType,
    Guid ProjectId,
    Guid? RepositoryId,
    Guid? ActorId,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    int AttemptCount,
    string InputHash,
    string CommitHash,
    string? SnapshotRef,
    string MetadataVersion) : IJobMessage;
```

Rules:

- `SchemaVersion` must be validated before deserialization-dependent work.
- `MessageId` must be unique per publish attempt.
- `JobId` is the durable source of truth.
- `CorrelationId` must flow into logs, activity logs, notifications, metrics, and engine calls.
- `InputHash` must be deterministic from job type, project id, repository id, revision/snapshot, and important parameters.
- Do not include Git credentials, secrets, tokens, raw stdout/stderr with secrets, or large payloads in messages. Store large inputs in PostgreSQL/MinIO and pass references.

## Job Record

A job row should be created before publishing the message. At minimum, persist:

```text
id, project_id, repository_id, type, status, progress,
input_hash, input_ref, result_ref, error_code, error_message,
attempt_count, max_attempts, correlation_id, actor_id,
created_at, queued_at, started_at, heartbeat_at, finished_at,
timeout_at, cancelled_at, metadata_version, schema_version
```

The database is the source of truth for job state. RabbitMQ is the transport, not the job database.

## Producer Flow

Use this sequence in Application/Service layer:

1. Authenticate and authorize the actor.
2. Validate project/repository state and input.
3. Compute `inputHash` / idempotency key.
4. Check whether an equivalent non-terminal job already exists.
5. Create a new `queued` job or return the existing job.
6. Save changes in a transaction.
7. Publish the message through an outbox or only after the transaction commits.
8. Emit activity log `job.queued`.
9. Return `202 Accepted` with `jobId`, `status`, and `correlationId`.

```csharp
public async Task<Result<JobDto>> QueueParseAsync(
    QueueParseRequest request,
    ActorContext actor,
    CancellationToken cancellationToken)
{
    await _authorization.RequireProjectPermissionAsync(
        actor.UserId,
        request.ProjectId,
        ProjectPermissions.AnalyzeProject,
        cancellationToken);

    var project = await _projects.GetByIdAsync(request.ProjectId, cancellationToken);
    if (project is null || project.IsDeleted)
        return Result.Fail(JobErrors.ProjectNotFound);

    var inputHash = JobInputHash.ForParse(
        request.ProjectId,
        request.RepositoryId,
        request.CommitHash,
        request.ParserOptions);

    var existing = await _jobs.FindActiveByInputHashAsync(inputHash, cancellationToken);
    if (existing is not null)
        return Result.Ok(JobDto.From(existing));

    var job = Job.Queue(
        JobTypes.Parse,
        request.ProjectId,
        request.RepositoryId,
        actor.UserId,
        inputHash,
        actor.CorrelationId,
        timeoutAt: _clock.UtcNow.Add(_options.ParseTimeout));

    _jobs.Add(job);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    await _publisher.PublishAsync(ParseProjectJobMessageFactory.Create(job, request), cancellationToken);
    await _activityLog.WriteAsync(ActivityEvents.JobQueued, job, actor, cancellationToken);

    return Result.Ok(JobDto.From(job));
}
```

## Consumer Flow

Every worker handler must follow this order:

1. Start a logging scope with `CorrelationId`, `JobId`, `ProjectId`, `RepositoryId`, `MessageId`, and `AttemptCount`.
2. Validate message schema and required fields.
3. Load the durable job row.
4. Ignore safely if the job is terminal or cancelled.
5. Verify message `InputHash` matches the job record.
6. Acquire Redis lock when reading/writing mutable repository workspace.
7. Mark job `running`, set `started_at` if first run, and update heartbeat.
8. Execute the engine with cancellation token, correlation id, actor context, and immutable input refs.
9. Upsert outputs idempotently.
10. Emit progress, metrics, activity log, notifications, and cache invalidation.
11. Mark terminal status or retry/dead-letter.
12. Release locks using the lock token, never by key only.

```csharp
public sealed class ParseProjectJobHandler : IJobHandler<ParseProjectJobMessage>
{
    public async Task HandleAsync(ParseProjectJobMessage message, CancellationToken ct)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = message.CorrelationId,
            ["JobId"] = message.JobId,
            ["ProjectId"] = message.ProjectId,
            ["RepositoryId"] = message.RepositoryId,
            ["MessageId"] = message.MessageId,
            ["AttemptCount"] = message.AttemptCount
        });

        var validation = _messageValidator.Validate(message);
        if (!validation.IsValid)
        {
            await DeadLetterAsync(message, JobErrors.InvalidMessageSchema, validation.Error, ct);
            return;
        }

        var job = await _jobs.GetForUpdateAsync(message.JobId, ct);
        if (job is null || job.IsTerminal || job.IsCancelled)
            return;

        if (!job.InputHashEquals(message.InputHash))
        {
            await DeadLetterAsync(message, JobErrors.StaleOrMismatchedMessage, "Input hash mismatch.", ct);
            return;
        }

        await using var repoLock = await _repositoryLocks.TryAcquireAsync(
            message.RepositoryId!.Value,
            message.CorrelationId,
            job.TimeoutRemaining,
            ct);

        if (!repoLock.Acquired)
        {
            await MarkRetryingAsync(job, JobErrors.RepositoryLocked, ct);
            throw new TransientJobException(JobErrors.RepositoryLocked.Code);
        }

        try
        {
            job.MarkRunning(_clock.UtcNow);
            await _jobs.SaveAsync(job, ct);
            await _activityLog.WriteJobStartedAsync(job, ct);

            var result = await _parserEngine.ParseAsync(new ParseInput(
                message.ProjectId,
                message.RepositoryId.Value,
                message.CommitHash,
                message.SnapshotRef,
                message.MetadataVersion,
                message.CorrelationId),
                progress: async progress =>
                {
                    job.UpdateProgress(progress.Percent, _clock.UtcNow);
                    await _jobs.SaveProgressAsync(job, ct);
                    await _progress.ReportAsync(job.ProjectId, job.Id, progress, message.CorrelationId, ct);
                },
                ct);

            await _metadata.UpsertParseResultAsync(result, ct);

            job.MarkCompleted(result.ResultRef, _clock.UtcNow);
            await _jobs.SaveAsync(job, ct);
            await _activityLog.WriteJobCompletedAsync(job, ct);
            await _notifications.JobCompletedAsync(job, ct);
            await _cacheInvalidator.InvalidateProjectMetadataAsync(job.ProjectId, ct);
        }
        catch (OperationCanceledException) when (job.IsCancellationRequested)
        {
            job.MarkCancelled(_clock.UtcNow);
            await _jobs.SaveAsync(job, ct);
            await _activityLog.WriteJobCancelledAsync(job, ct);
            await _notifications.JobCancelledAsync(job, ct);
        }
        catch (TimeoutException ex)
        {
            await HandleFailureAsync(job, JobErrors.Timeout, ex, ct);
            throw;
        }
        catch (Exception ex) when (_errorClassifier.IsTransient(ex))
        {
            await HandleFailureAsync(job, JobErrors.TransientDependencyFailure, ex, ct);
            throw;
        }
        catch (Exception ex)
        {
            await HandleFailureAsync(job, JobErrors.JobProcessingFailed, ex, ct);
        }
    }
}
```

## Lifecycle State Machine

Use these states and only these transitions:

| State | Meaning | Valid Next States |
| --- | --- | --- |
| `queued` | Job row exists and message has been or will be published. | `running`, `cancelled`, `failed`, `dead_lettered` |
| `running` | Worker is actively processing and heartbeat/progress should update. | `completed`, `failed`, `retrying`, `cancelled`, `timeout` |
| `retrying` | Transient failure; waiting for retry/backoff. | `queued`, `failed`, `dead_lettered`, `cancelled` |
| `completed` | Work completed successfully and outputs are committed. | Terminal |
| `failed` | Non-retryable failure or retries exhausted without DLQ. | Terminal; manual retry may create a new job |
| `cancelled` | User/system cancellation completed safely. | Terminal |
| `timeout` | Job exceeded timeout and was stopped or marked stale. | Terminal; manual retry may create a new job |
| `dead_lettered` | Poison message, schema error, or retry exhaustion requires inspection. | Manual inspect/requeue only |

Never allow stale worker completion to overwrite a newer project/repository state. Re-check service-layer state transitions before committing final results.

## Progress, Heartbeat, and SignalR

Workers must update progress and heartbeat for long jobs.

- Progress is 0-100 and must be monotonic.
- Heartbeat updates prove the worker is alive even when percent progress does not change.
- Progress events must include `jobId`, `projectId`, `status`, `progress`, `message`, `correlationId`, and timestamp.
- SignalR groups should be project-scoped: `project:{projectId}`.
- Do not use SignalR as the source of truth. The UI must be able to refresh from `GET /jobs/{id}`.

```csharp
public sealed class ProgressReporter : IProgressReporter
{
    private readonly IHubContext<GodForgeHub> _hubContext;

    public Task ReportAsync(
        Guid projectId,
        Guid jobId,
        JobProgress progress,
        string correlationId,
        CancellationToken ct)
    {
        return _hubContext.Clients
            .Group($"project:{projectId}")
            .SendAsync("JobProgressUpdate", new
            {
                jobId,
                projectId,
                status = progress.Status,
                progress = progress.Percent,
                message = progress.Message,
                correlationId,
                timestamp = progress.Timestamp
            }, ct);
    }
}
```

## Retry and DLQ Policy

Classify errors before choosing retry behavior.

| Error Class | Examples | Retry? | Final State |
| --- | --- | --- | --- |
| Transient dependency | temporary DB/MinIO/RabbitMQ/network failure | Yes, exponential backoff | `retrying`, then `queued`; `dead_lettered` after exhaustion |
| Timeout | job exceeded configured timeout | Maybe, only when policy allows | `timeout` or `retrying` |
| Poison message | invalid schema, impossible data, deserialization failure | No | `dead_lettered` |
| Data issue | scene syntax error, unsupported file, broken dependency | Usually no job-level retry; record issue and continue | Job may still `completed` with warnings |
| Authorization/state stale | project deleted, repository archived, input hash mismatch | No | `failed` or `dead_lettered` depending on case |
| Git credential error | invalid credential/token | No automatic retry | `failed` |
| Git conflict/rejected push | conflict, non-fast-forward, protected branch | No automatic retry | `failed` with actionable error |

Retry defaults:

- Maximum automatic attempts: 3 unless SRS/config says otherwise.
- Exponential backoff with jitter.
- Increment `attempt_count` and write `job.retrying` activity before requeue.
- Preserve original `JobId`, `InputHash`, and `CorrelationId` across retries.
- Create a new `MessageId` for each publish attempt.
- Move poison messages and retry-exhausted messages to the DLQ.

## Idempotency Rules

Every job must be safe to run more than once.

- Use `JobId` and `InputHash` to detect duplicate messages.
- Use upsert/replace-by-version, not blind insert, for metadata outputs.
- Store parser/analyzer output under deterministic `metadataVersion` or `{projectId}:{commitHash}` keys.
- Store MinIO artifacts with deterministic object keys when safe, or save result refs atomically.
- Do not create duplicate notifications for the same terminal transition.
- Do not create duplicate activity logs unless the event represents a real retry attempt.
- Git write operations are special: avoid automatic retry if retrying can duplicate commits/pushes. Prefer safe precondition checks and explicit user retry.

## Repository Locks

Use Redis distributed locks for jobs that read or mutate a mutable repository workspace.

Key format:

```text
repo-lock:{repositoryId}
```

Rules:

- Lock value must be a random owner token.
- TTL must match the job timeout or be renewed by heartbeat.
- Release must verify the owner token; never delete by key alone.
- Lock failure should usually become `retrying`, not immediate `failed`.
- Prefer immutable repository snapshots to reduce lock duration.

## Cancellation and Timeout

Cancellation must be cooperative:

- API marks job cancellation requested after checking RBAC.
- Worker checks `CancellationToken` and job cancellation flag at safe checkpoints.
- Worker must release locks and persist partial safe state.
- Cancelled jobs must emit activity log and notification.

Timeout handling:

- Each job type must have a configured timeout.
- Worker must update heartbeat periodically.
- A watchdog should mark stale `running` jobs as `timeout` or requeue according to policy.
- Timeout must not leave repository locks, temp files, partial artifacts, or half-committed metadata as active outputs.

## Output Storage

Use the right storage target:

- PostgreSQL: job state, metadata records, health issues, small diff summaries, activity, notifications.
- MinIO: large diff artifacts, preview snapshots, reports, repository archive/debug artifacts.
- Redis: short-lived locks, cache, job progress acceleration. Redis is not the source of truth.

Any artifact saved to MinIO must include metadata: `project_id`, `job_id`, `content_type`, `checksum`, `created_at`, and retention policy.

## Observability

Every worker must log structured events with:

```text
correlation_id, job_id, project_id, repository_id, actor_id,
job_type, status, attempt_count, duration_ms, error_code
```

Metrics should include:

- `worker_job_started_total{job_type}`
- `worker_job_completed_total{job_type}`
- `worker_job_failed_total{job_type,error_code}`
- `worker_job_retry_total{job_type}`
- `worker_job_duration_seconds{job_type}`
- `worker_queue_lag_seconds{queue}`
- `worker_active_jobs{job_type}`
- `worker_heartbeat_stale_total{job_type}`
- `worker_dlq_messages_total{queue}`

Never log secrets, credential tokens, raw authorization headers, full Git remote URLs with embedded credentials, or stack traces in API responses.

## Activity Log and Notifications

Write activity logs for important lifecycle events:

- `job.queued`
- `job.started`
- `job.progress` only for coarse milestones, not every percent
- `job.retrying`
- `job.completed`
- `job.failed`
- `job.cancelled`
- `job.timeout`
- `job.dead_lettered`

Notifications should be sent for final user-visible outcomes: completed, failed, timeout, dead-lettered, and cancelled when the actor is not the only audience.

## Cache Invalidation and Events

Emit domain events after durable state is committed:

- `RepositoryCloneCompleted` -> invalidate repository/dashboard state, request parse/analyze.
- `ParserCompleted` -> trigger analyze or publish metadata availability.
- `HealthReportPublished` -> invalidate dashboard/search facets and notify relevant members.
- `DiffCompleted` -> make diff result available to the requester.

Do not publish completion events before outputs are committed.

## Testing Requirements

Add tests at the correct layer:

- Application tests: permission check, state validation, idempotency, job creation, message publish, API response mapping.
- Worker handler tests: success path, missing job, terminal job ignored, stale input hash, cancellation, timeout, transient retry, poison DLQ, partial parse warnings.
- Infrastructure tests: RabbitMQ binding configuration, serialization schema, Redis lock owner-token release, repository implementation, MinIO artifact save.
- Integration tests: queue -> worker -> DB status -> SignalR/progress notification for at least one representative job.
- Regression tests for every bug fixed in worker behavior.

Minimum verification commands:

```bash
dotnet build
dotnet test
```

When touching RabbitMQ/Redis/MinIO behavior, also run the project integration test profile or docker-compose-based test environment if available.

## Implementation Checklist

- [ ] SRS job type, producer, consumer, timeout, retry, DLQ, and idempotency rules are confirmed.
- [ ] API/Application creates a durable job and returns `202 Accepted` with `jobId` and `correlationId`.
- [ ] Message includes `schemaVersion`, `messageId`, `jobId`, `jobType`, `projectId`, `repositoryId`, `actorId`, `correlationId`, `createdAt`, `attemptCount`, and `inputHash`.
- [ ] Message does not contain secrets or large payloads.
- [ ] Handler validates schema, job state, cancellation, and input hash before work.
- [ ] Handler uses correlation id in logging scope and downstream engine calls.
- [ ] Repository lock is used for mutable Git workspace access and released by owner token.
- [ ] Job lifecycle transitions are valid and persisted.
- [ ] Progress and heartbeat are updated; SignalR event is emitted for UI.
- [ ] Retry policy distinguishes transient errors, poison messages, data issues, timeout, and Git credential/conflict errors.
- [ ] DLQ path marks job `dead_lettered` when appropriate.
- [ ] Outputs are written idempotently to PostgreSQL/MinIO.
- [ ] Activity logs and notifications are written for lifecycle events.
- [ ] Dashboard/search cache invalidation happens after successful output commit.
- [ ] Unit/integration tests cover success, retry, DLQ, cancellation, timeout, idempotency, and lock behavior.
- [ ] `dotnet build` and `dotnet test` pass without warnings.

## Do Not

- Do not run clone, parse, analyze, diff, or preview synchronously inside HTTP requests when the work can exceed API latency targets.
- Do not treat RabbitMQ as the source of truth for job state.
- Do not swallow exceptions without updating job state and logs.
- Do not retry Git writes automatically when retry can duplicate side effects.
- Do not let one file parse error crash the whole parse job when partial parse is supported.
- Do not complete a stale worker job without checking current project/repository state.
- Do not log credentials, tokens, secrets, or stack traces to client responses.