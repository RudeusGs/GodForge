using GodForge.Domain.Entities.Identity;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IUserSessionRepository
{
    Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserSession?> GetActiveAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetValidUntilAsync(Guid id, Guid userId, string securityStamp, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSession>> GetForUserAsync(Guid userId, Guid currentSessionId, DateTimeOffset now, int limit, CancellationToken cancellationToken = default);
    Task<int> CountActiveForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task AddAsync(UserSession session, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> RevokeAllForUserAsync(Guid userId, string reason, DateTimeOffset now, CancellationToken cancellationToken = default);
}
