namespace GodForge.Application.Features.Analysis.DTOs;

public sealed record HealthIssueDto(
    Guid Id,
    string IssueType,
    string Severity,
    string? FilePath,
    string? NodePath,
    string Message,
    bool IsSuppressed);
