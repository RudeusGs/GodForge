using GodForge.Domain.Entities.Analysis;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IAiAnalysisRepository
{
    Task<AiAnalysisRun?> GetCompletedAsync(
        Guid repositoryId,
        string commitSha,
        string analysisProfile,
        string provider,
        string model,
        string promptVersion,
        string inputHash,
        CancellationToken cancellationToken = default);

    Task AddRunAsync(AiAnalysisRun run, CancellationToken cancellationToken = default);
    Task AddFindingAsync(AiFinding finding, CancellationToken cancellationToken = default);
}
