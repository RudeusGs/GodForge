using GodForge.Domain.Entities.Identity;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IAuthChallengeRepository
{
    Task<AuthChallenge?> GetActiveAsync(string normalizedEmail, string purpose, CancellationToken cancellationToken = default);
    Task<AuthChallenge?> GetBySecretHashAsync(string normalizedEmail, string purpose, string secretHash, CancellationToken cancellationToken = default);
    Task AddAsync(AuthChallenge challenge, CancellationToken cancellationToken = default);
}
