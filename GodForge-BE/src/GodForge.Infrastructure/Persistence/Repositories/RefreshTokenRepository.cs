using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly GodForgeDbContext _context;

    public RefreshTokenRepository(GodForgeDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);
    }

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(token, cancellationToken);
    }

    public Task DeleteAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _context.RefreshTokens.Remove(token);
        return Task.CompletedTask;
    }

    public async Task DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(token => token.UserId == userId)
            .ToListAsync(cancellationToken);
        _context.RefreshTokens.RemoveRange(tokens);
    }
}

