namespace GodForge.Application.Features.Auth.DTOs;

public record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    UserDto User);

public record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string SystemRole);
