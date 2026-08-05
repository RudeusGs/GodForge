using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class AuthChallengeRepository : IAuthChallengeRepository
{
    private readonly GodForgeDbContext _context;

    public AuthChallengeRepository(GodForgeDbContext context) => _context = context;

    public Task<AuthChallenge?> GetActiveAsync(string normalizedEmail, string purpose, CancellationToken cancellationToken = default)
        => _context.AuthChallenges
            .Where(x => x.NormalizedEmail == normalizedEmail && x.Purpose == purpose && x.ConsumedAt == null && x.RevokedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<AuthChallenge?> GetBySecretHashAsync(string normalizedEmail, string purpose, string secretHash, CancellationToken cancellationToken = default)
        => _context.AuthChallenges
            .Where(x => x.NormalizedEmail == normalizedEmail && x.Purpose == purpose && x.SecretHash == secretHash)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task AddAsync(AuthChallenge challenge, CancellationToken cancellationToken = default)
        => _context.AuthChallenges.AddAsync(challenge, cancellationToken).AsTask();
}
