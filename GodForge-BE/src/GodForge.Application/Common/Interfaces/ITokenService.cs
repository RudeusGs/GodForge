using GodForge.Domain.Entities.Identity;

namespace GodForge.Application.Common.Interfaces;

public interface ITokenService
{
    TimeSpan RefreshTokenLifetime { get; }
    AccessTokenResult GenerateAccessToken(User user, Guid sessionId, DateTimeOffset now);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
}

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);
