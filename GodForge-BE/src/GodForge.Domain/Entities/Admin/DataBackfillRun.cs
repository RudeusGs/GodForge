using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Admin;

public sealed class DataBackfillRun : BaseEntity
{
    public string Name { get; private set; } = default!;
    public RunStatus Status { get; private set; }
    public int ProcessedCount { get; private set; }
    public int FailedCount { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? MetadataJson { get; private set; }

    private DataBackfillRun() { } // EF Core

    public static DataBackfillRun Create(
        string name, DateTimeOffset now)
    {
        return new DataBackfillRun
        {
            Id = Guid.NewGuid(),
            Name = name,
            Status = RunStatus.Running,
            ProcessedCount = 0,
            FailedCount = 0,
            StartedAt = now
        };
    }

    public void MarkAsCompleted(int processedCount, int failedCount, DateTimeOffset now)
    {
        Status = RunStatus.Completed;
        ProcessedCount = processedCount;
        FailedCount = failedCount;
        CompletedAt = now;
    }

    public void MarkAsFailed(int processedCount, int failedCount, DateTimeOffset now)
    {
        Status = RunStatus.Failed;
        ProcessedCount = processedCount;
        FailedCount = failedCount;
        CompletedAt = now;
    }
}
