using GodForge.Domain.Entities.Analysis;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IDependencyGraphSnapshotRepository
{
    Task AddSnapshotAsync(DependencyGraphSnapshot snapshot, CancellationToken cancellationToken);
    Task AddNodesAsync(IEnumerable<DependencyGraphNode> nodes, CancellationToken cancellationToken);
    Task AddEdgesAsync(IEnumerable<DependencyGraphEdge> edges, CancellationToken cancellationToken);
    Task<DependencyGraphSnapshot?> GetLatestByProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DependencyGraphNode>> GetNodesBySnapshotAsync(Guid snapshotId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DependencyGraphEdge>> GetEdgesBySnapshotAsync(Guid snapshotId, CancellationToken cancellationToken);
}
