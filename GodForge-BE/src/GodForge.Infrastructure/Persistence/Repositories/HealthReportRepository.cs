using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Analysis;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class HealthReportRepository : IHealthReportRepository
{
    private readonly GodForgeDbContext _context;

    public HealthReportRepository(GodForgeDbContext context)
    {
        _context = context;
    }

    public async Task AddReportAsync(HealthReport report, CancellationToken cancellationToken)
    {
        await _context.HealthReports.AddAsync(report, cancellationToken);
    }

    public async Task AddIssuesAsync(IEnumerable<HealthIssue> issues, CancellationToken cancellationToken)
    {
        await _context.HealthIssues.AddRangeAsync(issues, cancellationToken);
    }

    public async Task<HealthReport?> GetLatestByProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _context.HealthReports
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HealthIssue>> GetIssuesByReportAsync(Guid reportId, CancellationToken cancellationToken)
    {
        return await _context.HealthIssues
            .Where(i => i.ReportId == reportId)
            .ToListAsync(cancellationToken);
    }
}
