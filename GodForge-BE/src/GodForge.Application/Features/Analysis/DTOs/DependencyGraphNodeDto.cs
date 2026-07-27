namespace GodForge.Application.Features.Analysis.DTOs;

public sealed record DependencyGraphNodeDto(
    string NodeKey,
    string NodeType,
    string? FilePath,
    string Label);
