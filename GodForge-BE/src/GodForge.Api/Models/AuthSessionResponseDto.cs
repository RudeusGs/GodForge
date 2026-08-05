using GodForge.Application.Features.Auth.DTOs;

namespace GodForge.Api.Models;

public sealed record AuthSessionResponseDto(
    UserDto User,
    SessionDto Session,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt)
{
    public static AuthSessionResponseDto From(AuthResultDto result) => new(
        result.User,
        result.Session,
        result.AccessToken,
        result.AccessTokenExpiresAt,
        result.RefreshTokenExpiresAt);
}
