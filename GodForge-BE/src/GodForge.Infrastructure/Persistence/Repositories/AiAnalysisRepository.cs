using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Analysis;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class AiAnalysisRepository : IAiAnalysisRepository
{
    private readonly GodForgeDbContext _context;

    public AiAnalysisRepository(GodForgeDbContext context)
    {
        _context = context;
    }

    public Task<AiAnalysisRun?> GetByInputHashAsync(
        Guid repositoryId,
        string commitSha,
        string analysisProfile,
        string inputHash,
        CancellationToken cancellationToken = default)
        => _context.AiAnalysisRuns.FirstOrDefaultAsync(
            run => run.RepositoryId == repositoryId &&
                   run.CommitSha == commitSha &&
                   run.AnalysisProfile == analysisProfile &&
                   run.InputHash == inputHash,
            cancellationToken);

    public async Task AddRunAsync(AiAnalysisRun run, CancellationToken cancellationToken = default)
        => await _context.AiAnalysisRuns.AddAsync(run, cancellationToken);

    public async Task AddFindingAsync(AiFinding finding, CancellationToken cancellationToken = default)
        => await _context.AiFindings.AddAsync(finding, cancellationToken);
}
