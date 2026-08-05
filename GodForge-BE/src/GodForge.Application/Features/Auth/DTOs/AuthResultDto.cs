using GodForge.Application.Common.Text;
using GodForge.Domain.Entities.Identity;

namespace GodForge.Application.Features.Auth.DTOs;

public sealed record AuthResultDto(
    UserDto User,
    SessionDto Session,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Status,
    DateTimeOffset? EmailVerifiedAt,
    DateTimeOffset CreatedAt,
    long Version)
{
    public static UserDto From(User user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        EnumText.ToCamelCase(user.Status),
        user.EmailVerifiedAt,
        user.CreatedAt,
        user.Version);
}

public sealed record SessionDto(
    Guid Id,
    string? DeviceName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset ExpiresAt,
    bool Current,
    DateTimeOffset? RevokedAt)
{
    public static SessionDto From(UserSession session, Guid? currentSessionId) => new(
        session.Id,
        session.DeviceName,
        session.CreatedAt,
        session.LastSeenAt,
        session.ExpiresAt,
        currentSessionId == session.Id,
        session.RevokedAt);
}

public sealed record ChallengeAcceptedDto(bool RequestAccepted, int ResendAfterSeconds);
