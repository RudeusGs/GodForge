namespace GodForge.Application.Features.Analysis.DTOs;

public sealed record HealthReportDto(
    Guid Id,
    Guid ProjectId,
    Guid RepositoryId,
    int Score,
    int TotalIssues,
    int CriticalCount,
    int WarningCount,
    int InfoCount,
    DateTimeOffset CreatedAt);
