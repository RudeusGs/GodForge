using GodForge.Application.Common.Models;
using GodForge.Domain.Entities.Core;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IOrganizationMemberRepository
{
    Task<OrganizationMember?> GetAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationMember>> GetForOrganizationsAsync(IReadOnlyCollection<Guid> organizationIds, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsActiveAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetActiveOwnerCountAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<PagedResult<OrganizationMember>> GetForOrganizationAsync(Guid organizationId, int page, int pageSize, string? role, string? status, string? search, CancellationToken cancellationToken = default);
    Task AddAsync(OrganizationMember membership, CancellationToken cancellationToken = default);
}
