using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Search;

public sealed class SearchIndexRun : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid? SnapshotId { get; private set; }
    public Guid? JobId { get; private set; }
    public RunStatus Status { get; private set; }
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
            Status = RunStatus.Running,
            DocumentCount = 0,
            StartedAt = now
        };
    }

    public void MarkAsCompleted(int documentCount, DateTimeOffset now)
    {
        Status = RunStatus.Completed;
        DocumentCount = documentCount;
        CompletedAt = now;
    }

    public void MarkAsFailed(DateTimeOffset now)
    {
        Status = RunStatus.Failed;
        CompletedAt = now;
    }
}
