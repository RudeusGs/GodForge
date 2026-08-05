using GodForge.Application.Common.Models;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, Guid? exceptId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<Organization>> GetForMemberAsync(Guid userId, int page, int pageSize, OrganizationStatus? status, CancellationToken cancellationToken = default);
    Task<int> CountCreatedByAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Organization organization, CancellationToken cancellationToken = default);
}
