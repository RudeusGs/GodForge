using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class UserSessionRepository : IUserSessionRepository
{
    private readonly GodForgeDbContext _context;

    public UserSessionRepository(GodForgeDbContext context) => _context = context;

    public Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.UserSessions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<UserSession?> GetActiveAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        => _context.UserSessions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.RevokedAt == null, cancellationToken);

    public Task<DateTimeOffset?> GetValidUntilAsync(
        Guid id,
        Guid userId,
        string securityStamp,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
        => _context.UserSessions
            .AsNoTracking()
            .Where(session =>
                session.Id == id &&
                session.UserId == userId &&
                session.RevokedAt == null &&
                session.ExpiresAt > now &&
                _context.Users.Any(user =>
                    user.Id == userId &&
                    user.Status == UserStatus.Active &&
                    user.SecurityStamp == securityStamp))
            .Select(session => (DateTimeOffset?)session.ExpiresAt)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<UserSession>> GetForUserAsync(
        Guid userId,
        Guid currentSessionId,
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        return await _context.UserSessions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Id == currentSessionId)
            .ThenByDescending(x => x.RevokedAt == null && x.ExpiresAt > now)
            .ThenByDescending(x => x.LastSeenAt ?? x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
        => _context.UserSessions.AddAsync(session, cancellationToken).AsTask();

    public Task<int> CountActiveForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
        => _context.UserSessions.CountAsync(
            session => session.UserId == userId && session.RevokedAt == null && session.ExpiresAt > now,
            cancellationToken);

    public async Task<IReadOnlyList<Guid>> RevokeAllForUserAsync(
        Guid userId,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _context.UserSessions
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
            session.Revoke(reason, now);

        return sessions.Select(session => session.Id).ToArray();
    }
}
