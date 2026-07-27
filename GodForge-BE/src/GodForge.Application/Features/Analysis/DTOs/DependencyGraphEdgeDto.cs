namespace GodForge.Application.Features.Analysis.DTOs;

public sealed record DependencyGraphEdgeDto(
    string SourceNodeKey,
    string TargetNodeKey,
    string Relation,
    decimal Weight);
