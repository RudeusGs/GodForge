using GodForge.Domain.Entities.Analysis;

namespace GodForge.Application.Common.Interfaces;

public interface IDependencyGraphBuilder
{
    Task<(DependencyGraphSnapshot Snapshot, IReadOnlyList<DependencyGraphNode> Nodes, IReadOnlyList<DependencyGraphEdge> Edges)> BuildAsync(
        Guid projectId,
        Guid repositoryId,
        Guid snapshotId,
        Guid analysisRunId,
        string workspacePath,
        CancellationToken cancellationToken = default);
}
