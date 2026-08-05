using GodForge.Application.Common.Models;
using GodForge.Domain.Entities.Core;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IProjectMemberRepository
{
    Task<ProjectMember?> GetMembershipAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
    Task<ProjectMember?> GetAnyMembershipAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectMember>> GetMembershipsAsync(IReadOnlyCollection<Guid> projectIds, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectMemberStatistics>> GetStatisticsAsync(IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken = default);
    Task AddAsync(ProjectMember member, CancellationToken cancellationToken = default);
    Task<int> GetOwnerCountAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<int> GetActiveCountAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<PagedResult<ProjectMember>> GetForProjectAsync(Guid projectId, int page, int pageSize, string? role, string? status, string? search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectMember>> GetActiveByOrganizationUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetSoleOwnerProjectIdsAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> SuspendAllForOrganizationUserAsync(Guid organizationId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> RemoveAllForOrganizationUserAsync(Guid organizationId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
