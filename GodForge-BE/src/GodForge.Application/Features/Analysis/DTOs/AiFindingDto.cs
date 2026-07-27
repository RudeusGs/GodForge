namespace GodForge.Application.Features.Analysis.DTOs;

public sealed record AiFindingDto(
    Guid Id,
    string Category,
    string Severity,
    string Title,
    string Description,
    string? Recommendation,
    decimal? Confidence,
    string EvidenceRefsJson,
    string Status,
    DateTimeOffset CreatedAt);
