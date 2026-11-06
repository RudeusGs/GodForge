using GodForge.Application.Common.Models;
using GodForge.Application.Common.Models.Analysis;

namespace GodForge.Application.Common.Interfaces;

public interface IHostedGitService
{
    bool IsConfigured { get; }
    Task<long> EnsureWebhookAsync(string providerId, long? managedHookId, CancellationToken cancellationToken);
    Task<ForgejoUserDto> VerifyIdentityAsync(string personalToken, CancellationToken cancellationToken);
    Task<IReadOnlyList<ForgejoCollaboratorDto>> CollaboratorsAsync(string providerId, CancellationToken cancellationToken);
    Task SetCollaboratorAsync(string providerId, long userId, string login, string? permission, CancellationToken cancellationToken);
    Task<bool> ProtectionAsync(string providerId, string branch, bool? enabled, CancellationToken cancellationToken);
    Task<HostedRepositoryProvisionResult> ProvisionAsync(
        HostedRepositoryProvisionRequest request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RepositoryBranchDto>> BranchesAsync(string providerId, int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<RepositoryCommitDto>> CommitsAsync(string providerId, string branch, int page, int pageSize, CancellationToken cancellationToken);
    Task<RepositoryTreeDto> TreeAsync(string providerId, string commitSha, string path, CancellationToken cancellationToken);
    Task<RepositoryBlobDto> BlobAsync(string providerId, string commitSha, string path, CancellationToken cancellationToken);
}
