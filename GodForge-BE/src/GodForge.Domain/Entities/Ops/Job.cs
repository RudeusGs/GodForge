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
    public Guid? ClaimToken { get; private set; }
    public DateTimeOffset? CancellationRequestedAt { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? TriggeredBy { get; private set; }
    public string CorrelationId { get; private set; } = default!;

    private Job() { }

    public static Job Create(Guid projectId, Guid? repositoryId, JobType type, string queueName, int priority, string? payload, string? idempotencyKey, int maxAttempts, Guid? triggeredBy, string correlationId, DateTimeOffset availableAt, DateTimeOffset now)
    {
        EnumGuard.ThrowIfUndefined(type, nameof(type));
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "A job must allow at least one attempt.");

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
        EnsureStatus(JobStatus.Queued, JobStatus.Retrying);
        if (AvailableAt > now)
            throw new InvalidOperationException("A job cannot run before its available time.");

        Status = JobStatus.Running;
        StartedAt ??= now;
        LastHeartbeatAt = now;
        ClaimToken = Guid.NewGuid();
        AttemptCount++;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAt = now;
    }

    public void UpdateProgress(int progress, DateTimeOffset now)
    {
        EnsureStatus(JobStatus.Running);
        if (progress is < 0 or > 99)
            throw new ArgumentOutOfRangeException(nameof(progress), "Running job progress must be between 0 and 99.");
        if (progress < Progress)
            throw new InvalidOperationException("Job progress cannot move backwards.");

        Progress = progress;
        LastHeartbeatAt = now;
        UpdatedAt = now;
    }

    public void MarkCompleted(string? result, DateTimeOffset now)
    {
        EnsureStatus(JobStatus.Running);
        Status = JobStatus.Completed;
        Progress = 100;
        Result = result;
        ErrorCode = null;
        ErrorMessage = null;
        CompletedAt = now;
        LastHeartbeatAt = now;
        ClaimToken = null;
        UpdatedAt = now;
    }

    public void MarkFailed(string errorCode, string errorMessage, DateTimeOffset now)
    {
        EnsureStatus(JobStatus.Running);
        SetFailure(JobStatus.Failed, errorCode, errorMessage, now);
    }

    public void MarkRetrying(string errorCode, string errorMessage, DateTimeOffset availableAt, DateTimeOffset now)
    {
        EnsureStatus(JobStatus.Running);
        if (availableAt <= now)
            throw new ArgumentOutOfRangeException(nameof(availableAt), "A retry must be scheduled in the future.");

        Status = JobStatus.Retrying;
        ErrorCode = Required(errorCode, nameof(errorCode));
        ErrorMessage = Required(errorMessage, nameof(errorMessage));
        AvailableAt = availableAt;
        LastHeartbeatAt = now;
        ClaimToken = null;
        UpdatedAt = now;
    }

    public void MarkDeadLettered(string errorCode, string errorMessage, DateTimeOffset now)
    {
        EnsureStatus(JobStatus.Running, JobStatus.Queued, JobStatus.Retrying);
        SetFailure(JobStatus.DeadLettered, errorCode, errorMessage, now);
    }

    public void RequestCancellation(DateTimeOffset now)
    {
        if (IsTerminal(Status))
            return;

        CancellationRequestedAt ??= now;
        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (IsTerminal(Status))
            return;

        Status = JobStatus.Cancelled;
        CancelledAt = now;
        LastHeartbeatAt = now;
        ClaimToken = null;
        UpdatedAt = now;
    }

    private void SetFailure(JobStatus status, string errorCode, string errorMessage, DateTimeOffset now)
    {
        Status = status;
        ErrorCode = Required(errorCode, nameof(errorCode));
        ErrorMessage = Required(errorMessage, nameof(errorMessage));
        CompletedAt = now;
        LastHeartbeatAt = now;
        ClaimToken = null;
        UpdatedAt = now;
    }

    private void EnsureStatus(params JobStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new InvalidOperationException($"Job transition is not valid from status '{Status}'.");
    }

    private static bool IsTerminal(JobStatus status)
        => status is JobStatus.Completed or JobStatus.Failed or JobStatus.Cancelled or JobStatus.Timeout or JobStatus.DeadLettered;

    private static string Required(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
