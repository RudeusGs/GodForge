using GodForge.Domain.Entities.Identity;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IUserSessionRepository
{
    Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserSession?> GetActiveAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsValidAsync(Guid id, Guid userId, string securityStamp, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSession>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserSession session, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, string reason, DateTimeOffset now, CancellationToken cancellationToken = default);
}
