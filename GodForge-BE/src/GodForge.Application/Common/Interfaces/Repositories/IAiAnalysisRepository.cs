using GodForge.Domain.Entities.Analysis;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IAiAnalysisRepository
{
    Task<AiAnalysisRun?> GetByInputHashAsync(
        Guid repositoryId,
        string commitSha,
        string analysisProfile,
        string inputHash,
        CancellationToken cancellationToken = default);

    Task AddRunAsync(AiAnalysisRun run, CancellationToken cancellationToken = default);
    Task AddFindingAsync(AiFinding finding, CancellationToken cancellationToken = default);
}
