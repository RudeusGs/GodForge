using GodForge.Application.Common.Models.Analysis;

namespace GodForge.Application.Common.Interfaces;

public interface IRepositoryWorkspaceService
{
    Task<WorkspaceSyncResult> SyncAsync(
        Guid repositoryId,
        string remoteUrl,
        string branch,
        CancellationToken cancellationToken = default);
}
