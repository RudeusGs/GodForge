using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Analysis;

public sealed class DependencyGraphNode : BaseEntity
{
    public Guid GraphSnapshotId { get; private set; }
    public string NodeKey { get; private set; } = default!;
    public string NodeType { get; private set; } = default!;
    public string? FilePath { get; private set; }
    public string Label { get; private set; } = default!;
    public string? MetricsJson { get; private set; }

    private DependencyGraphNode() { } // EF Core

    public static DependencyGraphNode Create(
        Guid graphSnapshotId, string nodeKey, string nodeType,
        string? filePath, string label, string? metricsJson)
    {
        return new DependencyGraphNode
        {
            Id = Guid.NewGuid(),
            GraphSnapshotId = graphSnapshotId,
            NodeKey = nodeKey,
            NodeType = nodeType,
            FilePath = filePath,
            Label = label,
            MetricsJson = metricsJson
        };
    }
}
