using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Analysis;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class AnalysisRunRepository : IAnalysisRunRepository
{
    private readonly GodForgeDbContext _context;

    public AnalysisRunRepository(GodForgeDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AnalysisRun run, CancellationToken cancellationToken)
    {
        await _context.AnalysisRuns.AddAsync(run, cancellationToken);
    }
}
