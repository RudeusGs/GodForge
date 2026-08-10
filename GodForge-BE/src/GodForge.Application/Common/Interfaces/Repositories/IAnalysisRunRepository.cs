using GodForge.Domain.Entities.Analysis;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IAnalysisRunRepository
{
    Task<AnalysisRun?> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken);
    Task AddAsync(AnalysisRun run, CancellationToken cancellationToken);
}
