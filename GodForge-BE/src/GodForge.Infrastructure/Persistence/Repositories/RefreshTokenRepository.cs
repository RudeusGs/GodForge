using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly GodForgeDbContext _context;

    public RefreshTokenRepository(GodForgeDbContext context) => _context = context;

    public Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken cancellationToken = default)
        => _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);

    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
        => _context.RefreshTokens.AddAsync(token, cancellationToken).AsTask();

    public async Task RevokeAllForSessionAsync(Guid sessionId, string reason, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens.Where(x => x.SessionId == sessionId && x.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.Revoke(now, reason);
    }

    public async Task RevokeAllForFamilyAsync(Guid familyId, string reason, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens.Where(x => x.FamilyId == familyId && x.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.Revoke(now, reason);
    }

    public async Task RevokeAllForUserAsync(Guid userId, string reason, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.Revoke(now, reason);
    }

    public Task DeleteAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _context.RefreshTokens.Remove(token);
        return Task.CompletedTask;
    }

    public async Task DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens.Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        _context.RefreshTokens.RemoveRange(tokens);
    }
}
