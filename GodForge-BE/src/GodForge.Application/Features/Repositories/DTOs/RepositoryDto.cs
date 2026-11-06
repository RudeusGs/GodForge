using GodForge.Domain.Entities.Repo;

namespace GodForge.Application.Features.Repositories.DTOs;

public sealed record RepositoryDto(
    Guid Id,
    Guid ProjectId,
    string Mode,
    string Provider,
    string CloneUrl,
    string DefaultBranch,
    string Status,
    bool AutoAnalyzeEnabled,
    string? CurrentCommitHash,
    long? RepositorySizeBytes,
    DateTimeOffset? LastSyncedAt,
    string? LastErrorCode,
    DateTimeOffset? AccessRequestedAt = null,
    DateTimeOffset? AccessReconciledAt = null,
    string? AccessErrorCode = null,
    long AccessGeneration = 0,
    long AccessConfirmedGeneration = 0,
    DateTimeOffset? WebhookConfirmedAt = null)
{
    public static RepositoryDto From(GitRepository repository)
        => new(
            repository.Id,
            repository.ProjectId,
            repository.Mode.ToString(),
            repository.Provider.ToString(),
            repository.CloneUrlSanitized,
            repository.DefaultBranch,
            repository.GitRepositoryStatus.ToString(),
            repository.AutoAnalyzeEnabled,
            repository.CurrentCommitHash,
            repository.RepoSizeBytes,
            repository.LastSyncedAt,
            repository.LastErrorCode,
            repository.AccessRequestedAt,
            repository.AccessReconciledAt,
            repository.AccessErrorCode,
            repository.AccessGeneration,
            repository.AccessConfirmedGeneration,
            repository.WebhookConfirmedAt);
}
