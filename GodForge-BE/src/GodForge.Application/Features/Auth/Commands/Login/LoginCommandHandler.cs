using MediatR;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Enums;

namespace GodForge.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<DTOs.AuthResultDto>>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DTOs.AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
            return ApplicationError.Unauthorized("INVALID_CREDENTIALS", "Invalid email or password.");

        if (user.Status == UserStatus.Disabled)
            return ApplicationError.Forbidden("ACCOUNT_DISABLED", "Account is disabled.");

        if (user.Status == UserStatus.Locked && user.LockedUntil > now)
            return ApplicationError.Forbidden("ACCOUNT_LOCKED", "Account is temporarily locked.");

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.RecordLoginFailure(now, 5, TimeSpan.FromMinutes(15));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationError.Unauthorized("INVALID_CREDENTIALS", "Invalid email or password.");
        }

        user.RecordLoginSuccess(now);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var hashedRefreshToken = _tokenService.HashRefreshToken(refreshToken);

        var tokenEntity = GodForge.Domain.Entities.Identity.RefreshToken.Create(
            user.Id,
            hashedRefreshToken,
            null, // deviceName
            null, // ipAddress
            now.AddDays(7),
            now);

        await _refreshTokens.AddAsync(tokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var userDto = new DTOs.UserDto(user.Id, user.Email, user.DisplayName, user.SystemRole.ToString());
        return new DTOs.AuthResultDto(accessToken, refreshToken, userDto);
    }
}
