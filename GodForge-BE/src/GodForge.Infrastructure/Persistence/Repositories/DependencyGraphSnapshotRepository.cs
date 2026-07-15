using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Analysis;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class DependencyGraphSnapshotRepository : IDependencyGraphSnapshotRepository
{
    private readonly GodForgeDbContext _context;

    public DependencyGraphSnapshotRepository(GodForgeDbContext context)
    {
        _context = context;
    }

    public async Task AddSnapshotAsync(DependencyGraphSnapshot snapshot, CancellationToken cancellationToken)
    {
        await _context.DependencyGraphSnapshots.AddAsync(snapshot, cancellationToken);
    }

    public async Task AddNodesAsync(IEnumerable<DependencyGraphNode> nodes, CancellationToken cancellationToken)
    {
        await _context.DependencyGraphNodes.AddRangeAsync(nodes, cancellationToken);
    }

    public async Task AddEdgesAsync(IEnumerable<DependencyGraphEdge> edges, CancellationToken cancellationToken)
    {
        await _context.DependencyGraphEdges.AddRangeAsync(edges, cancellationToken);
    }

    public async Task<DependencyGraphSnapshot?> GetLatestByProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _context.DependencyGraphSnapshots
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DependencyGraphNode>> GetNodesBySnapshotAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        return await _context.DependencyGraphNodes
            .Where(n => n.GraphSnapshotId == snapshotId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DependencyGraphEdge>> GetEdgesBySnapshotAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        return await _context.DependencyGraphEdges
            .Where(e => e.GraphSnapshotId == snapshotId)
            .ToListAsync(cancellationToken);
    }
}
