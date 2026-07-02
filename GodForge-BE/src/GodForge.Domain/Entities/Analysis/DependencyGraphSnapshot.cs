using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Analysis;

public sealed class DependencyGraphSnapshot : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid RepositoryId { get; private set; }
    public Guid SnapshotId { get; private set; }
    public Guid AnalysisRunId { get; private set; }
    public string GraphHash { get; private set; } = default!;
    public int NodeCount { get; private set; }
    public int EdgeCount { get; private set; }
    public int CycleCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private DependencyGraphSnapshot() { } // EF Core

    public static DependencyGraphSnapshot Create(
        Guid projectId, Guid repositoryId, Guid snapshotId,
        Guid analysisRunId, string graphHash, int nodeCount,
        int edgeCount, int cycleCount, DateTimeOffset now)
    {
        return new DependencyGraphSnapshot
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RepositoryId = repositoryId,
            SnapshotId = snapshotId,
            AnalysisRunId = analysisRunId,
            GraphHash = graphHash,
            NodeCount = nodeCount,
            EdgeCount = edgeCount,
            CycleCount = cycleCount,
            CreatedAt = now
        };
    }
}
