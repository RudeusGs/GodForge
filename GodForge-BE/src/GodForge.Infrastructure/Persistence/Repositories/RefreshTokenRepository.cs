using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities;
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
}
