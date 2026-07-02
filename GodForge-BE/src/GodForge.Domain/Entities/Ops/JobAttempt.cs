using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Ops;

public sealed class JobAttempt : BaseEntity
{
    public Guid JobId { get; private set; }
    public int AttemptNumber { get; private set; }
    public string? WorkerName { get; private set; }
    public string? WorkerInstanceId { get; private set; }
    public string Status { get; private set; } = default!;
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int? DurationMs { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? StackTraceHash { get; private set; }

    private JobAttempt() { } // EF Core

    public static JobAttempt Create(Guid jobId, int attemptNumber, string? workerName, string? workerInstanceId, DateTimeOffset now)
    {
        return new JobAttempt
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            AttemptNumber = attemptNumber,
            WorkerName = workerName,
            WorkerInstanceId = workerInstanceId,
            Status = "running",
            StartedAt = now
        };
    }

    public void MarkAsCompleted(int durationMs, DateTimeOffset now)
    {
        Status = "completed";
        DurationMs = durationMs;
        CompletedAt = now;
    }

    public void MarkAsFailed(string? errorCode, string? errorMessage, string? stackTraceHash, int durationMs, DateTimeOffset now)
    {
        Status = "failed";
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        StackTraceHash = stackTraceHash;
        DurationMs = durationMs;
        CompletedAt = now;
    }
}
