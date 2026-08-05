using GodForge.Application.Common.Models;
using GodForge.Domain.Entities.Identity;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IUserInviteRepository
{
    Task<UserInvite?> GetByIdAsync(Guid organizationId, Guid invitationId, CancellationToken cancellationToken = default);
    Task<UserInvite?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<UserInvite?> GetPendingAsync(Guid organizationId, string normalizedEmail, CancellationToken cancellationToken = default);
    Task<PagedResult<UserInvite>> GetForOrganizationAsync(Guid organizationId, int page, int pageSize, string? status, string? email, CancellationToken cancellationToken = default);
    Task<int> CountPendingAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task AddAsync(UserInvite invitation, CancellationToken cancellationToken = default);
}
