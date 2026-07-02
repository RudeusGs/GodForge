using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Search;

public sealed class SearchIndexRun : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid? SnapshotId { get; private set; }
    public Guid? JobId { get; private set; }
    public string Status { get; private set; } = default!;
    public int DocumentCount { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private SearchIndexRun() { } // EF Core

    public static SearchIndexRun Create(
        Guid projectId, Guid? snapshotId, Guid? jobId, DateTimeOffset now)
    {
        return new SearchIndexRun
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotId = snapshotId,
            JobId = jobId,
            Status = "running",
            DocumentCount = 0,
            StartedAt = now
        };
    }

    public void MarkAsCompleted(int documentCount, DateTimeOffset now)
    {
        Status = "completed";
        DocumentCount = documentCount;
        CompletedAt = now;
    }

    public void MarkAsFailed(DateTimeOffset now)
    {
        Status = "failed";
        CompletedAt = now;
    }
}
