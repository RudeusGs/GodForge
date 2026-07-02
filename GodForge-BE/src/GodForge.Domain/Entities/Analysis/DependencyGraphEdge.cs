using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Analysis;

public sealed class DependencyGraphEdge : BaseEntity
{
    public Guid GraphSnapshotId { get; private set; }
    public string SourceNodeKey { get; private set; } = default!;
    public string TargetNodeKey { get; private set; } = default!;
    public string Relation { get; private set; } = default!;
    public decimal Weight { get; private set; }

    private DependencyGraphEdge() { } // EF Core

    public static DependencyGraphEdge Create(
        Guid graphSnapshotId, string sourceNodeKey,
        string targetNodeKey, string relation, decimal weight)
    {
        return new DependencyGraphEdge
        {
            Id = Guid.NewGuid(),
            GraphSnapshotId = graphSnapshotId,
            SourceNodeKey = sourceNodeKey,
            TargetNodeKey = targetNodeKey,
            Relation = relation,
            Weight = weight
        };
    }
}
