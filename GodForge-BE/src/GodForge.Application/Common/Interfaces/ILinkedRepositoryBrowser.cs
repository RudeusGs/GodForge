using GodForge.Application.Common.Models;
using GodForge.Application.Common.Models.Analysis;

namespace GodForge.Application.Common.Interfaces;

public interface ILinkedRepositoryBrowser
{
    Task<WorkspaceSyncResult> SynchronizeAsync(Guid repositoryId, string remoteUrl, string defaultBranch, CancellationToken token);
    Task<IReadOnlyList<RepositoryBranchDto>> BranchesAsync(Guid repositoryId, string defaultBranch, int page, int pageSize, CancellationToken token);
    Task<IReadOnlyList<RepositoryCommitDto>> CommitsAsync(Guid repositoryId, string branch, int page, int pageSize, CancellationToken token);
    Task<RepositoryTreeDto> TreeAsync(Guid repositoryId, string sha, string path, CancellationToken token);
    Task<RepositoryBlobDto> BlobAsync(Guid repositoryId, string sha, string path, CancellationToken token);
}
