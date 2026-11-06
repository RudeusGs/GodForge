using GodForge.Application.Common.Models;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;

namespace GodForge.Application.Common.Interfaces;

public sealed record HostedAccessCandidate(long ProviderUserId, string Login, ProjectRole Role);
public sealed record HostedAccessScope(GitRepository Repository, Guid OrganizationId, IReadOnlyList<HostedAccessCandidate> Members);
public interface IHostedRepositoryAccessStore
{
    Task<ForgejoIdentity?> GetIdentityAsync(Guid userId, CancellationToken cancellationToken);
    Task AddIdentityAsync(ForgejoIdentity identity, CancellationToken cancellationToken);
    Task<HostedAccessScope?> ReadAsync(Guid repositoryId, CancellationToken cancellationToken);
}
