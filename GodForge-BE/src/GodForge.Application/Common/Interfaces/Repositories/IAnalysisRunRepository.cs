using GodForge.Domain.Entities.Analysis;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IAnalysisRunRepository
{
    Task AddAsync(AnalysisRun run, CancellationToken cancellationToken);
}
