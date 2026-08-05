using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Identity;
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

    public async Task<IReadOnlyList<UserSession>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.UserSessions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastSeenAt ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
        => _context.UserSessions.AddAsync(session, cancellationToken).AsTask();

    public async Task RevokeAllForUserAsync(Guid userId, string reason, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var sessions = await _context.UserSessions
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
            session.Revoke(reason, now);
    }
}
