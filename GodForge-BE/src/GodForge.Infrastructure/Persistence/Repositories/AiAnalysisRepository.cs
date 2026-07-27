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

    public Task<AiAnalysisRun?> GetCompletedAsync(
        Guid repositoryId,
        string commitSha,
        string analysisProfile,
        string provider,
        string model,
        string promptVersion,
        string inputHash,
        CancellationToken cancellationToken = default)
        => _context.AiAnalysisRuns
            .AsNoTracking()
            .Where(run => run.Status == "completed")
            .OrderByDescending(run => run.CompletedAt)
            .FirstOrDefaultAsync(
                run => run.RepositoryId == repositoryId &&
                       run.CommitSha == commitSha &&
                       run.AnalysisProfile == analysisProfile &&
                       run.Provider == provider &&
                       run.Model == model &&
                       run.PromptVersion == promptVersion &&
                       run.InputHash == inputHash,
                cancellationToken);

    public Task<AiAnalysisRun?> GetLatestByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
        => _context.AiAnalysisRuns
            .AsNoTracking()
            .Where(r => r.ProjectId == projectId && r.Status == "completed")
            .OrderByDescending(r => r.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AiFinding>> GetFindingsByRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        return await _context.AiFindings
            .AsNoTracking()
            .Where(f => f.RunId == runId)
            .OrderByDescending(f => f.Severity == "critical")
            .ThenByDescending(f => f.Severity == "warning")
            .ThenByDescending(f => f.Severity == "info")
            .ThenBy(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRunAsync(AiAnalysisRun run, CancellationToken cancellationToken = default)
        => await _context.AiAnalysisRuns.AddAsync(run, cancellationToken);

    public async Task AddFindingAsync(AiFinding finding, CancellationToken cancellationToken = default)
        => await _context.AiFindings.AddAsync(finding, cancellationToken);
}
