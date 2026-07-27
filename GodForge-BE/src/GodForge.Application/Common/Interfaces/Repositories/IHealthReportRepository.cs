using GodForge.Domain.Entities.Analysis;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IHealthReportRepository
{
    Task AddReportAsync(HealthReport report, CancellationToken cancellationToken);
    Task AddIssuesAsync(IEnumerable<HealthIssue> issues, CancellationToken cancellationToken);
    Task<HealthReport?> GetLatestByProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<HealthIssue>> GetIssuesByReportAsync(Guid reportId, CancellationToken cancellationToken);
}
