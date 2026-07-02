using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Admin;

public sealed class DataBackfillRun : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Status { get; private set; } = default!;
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
            Status = "running",
            ProcessedCount = 0,
            FailedCount = 0,
            StartedAt = now
        };
    }

    public void MarkAsCompleted(int processedCount, int failedCount, DateTimeOffset now)
    {
        Status = "completed";
        ProcessedCount = processedCount;
        FailedCount = failedCount;
        CompletedAt = now;
    }

    public void MarkAsFailed(int processedCount, int failedCount, DateTimeOffset now)
    {
        Status = "failed";
        ProcessedCount = processedCount;
        FailedCount = failedCount;
        CompletedAt = now;
    }
}
