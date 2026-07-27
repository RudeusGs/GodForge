namespace GodForge.Application.Features.Analysis.DTOs;

public sealed record DependencyGraphDto(
    Guid SnapshotId,
    string GraphHash,
    int NodeCount,
    int EdgeCount,
    int CycleCount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<DependencyGraphNodeDto> Nodes,
    IReadOnlyList<DependencyGraphEdgeDto> Edges);
