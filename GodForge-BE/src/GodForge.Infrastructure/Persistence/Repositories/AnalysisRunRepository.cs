using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Analysis;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class AnalysisRunRepository : IAnalysisRunRepository
{
    private readonly GodForgeDbContext _context;

    public AnalysisRunRepository(GodForgeDbContext context)
    {
        _context = context;
    }

    public Task<AnalysisRun?> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken)
        => _context.AnalysisRuns.FirstOrDefaultAsync(run => run.JobId == jobId, cancellationToken);

    public async Task AddAsync(AnalysisRun run, CancellationToken cancellationToken)
    {
        await _context.AnalysisRuns.AddAsync(run, cancellationToken);
    }
}
