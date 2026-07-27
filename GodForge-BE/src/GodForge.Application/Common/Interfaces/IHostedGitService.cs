using GodForge.Application.Common.Models.Analysis;

namespace GodForge.Application.Common.Interfaces;

public interface IHostedGitService
{
    Task<HostedRepositoryProvisionResult> ProvisionAsync(
        HostedRepositoryProvisionRequest request,
        CancellationToken cancellationToken = default);
}
