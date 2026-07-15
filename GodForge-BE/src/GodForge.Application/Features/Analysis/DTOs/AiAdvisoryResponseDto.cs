namespace GodForge.Application.Features.Analysis.DTOs;

public sealed record AiAdvisoryResponseDto(
    AiAdvisoryDto Advisory,
    IReadOnlyList<AiFindingDto> Findings);

public sealed record AiAdvisoryDto(
    Guid Id,
    Guid ProjectId,
    Guid RepositoryId,
    string CommitSha,
    string Provider,
    string Model,
    string Status,
    string? Summary,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);
