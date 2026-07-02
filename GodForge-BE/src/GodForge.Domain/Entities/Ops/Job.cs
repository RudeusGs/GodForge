using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Ops;

public sealed class Job : BaseAuditableEntity
{
    public Guid ProjectId { get; private set; }
    public Guid? RepositoryId { get; private set; }
    public JobType Type { get; private set; }
    public JobStatus Status { get; private set; }
    public string QueueName { get; private set; } = null!;
    public int Priority { get; private set; }
    public int Progress { get; private set; }
    public string? Payload { get; private set; }
    public string? Result { get; private set; }
    public string? Metadata { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public int MaxAttempts { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset AvailableAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public DateTimeOffset? TimeoutAt { get; private set; }
    public DateTimeOffset? LastHeartbeatAt { get; private set; }
    public DateTimeOffset? CancellationRequestedAt { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? TriggeredBy { get; private set; }
    public string CorrelationId { get; private set; } = default!;

    private Job() { }

    public static Job Create(Guid projectId, Guid? repositoryId, JobType type, string queueName, int priority, string? payload, string? idempotencyKey, int maxAttempts, Guid? triggeredBy, string correlationId, DateTimeOffset availableAt, DateTimeOffset now)
    {
        return new Job
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RepositoryId = repositoryId,
            Type = type,
            Status = JobStatus.Queued,
            QueueName = queueName,
            Priority = priority,
            Progress = 0,
            Payload = payload,
            IdempotencyKey = idempotencyKey,
            MaxAttempts = maxAttempts,
            AttemptCount = 0,
            AvailableAt = availableAt,
            TriggeredBy = triggeredBy,
            CorrelationId = correlationId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkRunning(DateTimeOffset now)
    {
        Status = JobStatus.Running;
        StartedAt ??= now;
        LastHeartbeatAt = now;
        AttemptCount++;
        UpdatedAt = now;
    }

    public void UpdateProgress(int progress, DateTimeOffset now)
    {
        Progress = progress;
        LastHeartbeatAt = now;
        UpdatedAt = now;
    }

    public void MarkCompleted(string? result, DateTimeOffset now)
    {
        Status = JobStatus.Completed;
        Progress = 100;
        Result = result;
        CompletedAt = now;
        LastHeartbeatAt = now;
        UpdatedAt = now;
    }

    public void MarkFailed(string errorCode, string errorMessage, DateTimeOffset now)
    {
        Status = JobStatus.Failed;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        CompletedAt = now; // SRS doesn't have FailedAt, so we use CompletedAt or just UpdatedAt.
        LastHeartbeatAt = now;
        UpdatedAt = now;
    }

    public void RequestCancellation(DateTimeOffset now)
    {
        if (Status is JobStatus.Completed or JobStatus.Failed or JobStatus.Cancelled or JobStatus.DeadLettered)
            return;

        CancellationRequestedAt = now;
        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status is JobStatus.Completed or JobStatus.Failed or JobStatus.Cancelled or JobStatus.DeadLettered)
            return;

        Status = JobStatus.Cancelled;
        CancelledAt = now;
        LastHeartbeatAt = now;
        UpdatedAt = now;
    }
}
