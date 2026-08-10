using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Analysis;

public sealed class AnalysisRun : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid RepositoryId { get; private set; }
    public Guid SnapshotId { get; private set; }
    public Guid? MetadataRunId { get; private set; }
    public Guid? JobId { get; private set; }
    public RunStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private AnalysisRun() { } // EF Core

    public static AnalysisRun Create(
        Guid projectId, Guid repositoryId, Guid snapshotId,
        Guid? metadataRunId, Guid? jobId, DateTimeOffset now)
    {
        return new AnalysisRun
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RepositoryId = repositoryId,
            SnapshotId = snapshotId,
            MetadataRunId = metadataRunId,
            JobId = jobId,
            Status = RunStatus.Running,
            StartedAt = now
        };
    }

    public void MarkAsCompleted(DateTimeOffset now)
    {
        Status = RunStatus.Completed;
        CompletedAt = now;
    }

    public void MarkAsFailed(DateTimeOffset now)
    {
        Status = RunStatus.Failed;
        CompletedAt = now;
    }
}
