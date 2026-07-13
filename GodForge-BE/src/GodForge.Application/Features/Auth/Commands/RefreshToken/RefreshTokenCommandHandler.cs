using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<AuthResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hashedToken = _tokenService.HashRefreshToken(request.RefreshToken);
        var tokenEntity = await _refreshTokenRepository.GetByHashAsync(hashedToken, cancellationToken);

        if (tokenEntity is null)
        {
            return Result<AuthResultDto>.Failure(ApplicationError.Unauthorized("AUTH_INVALID_TOKEN", "Invalid refresh token."));
        }

        var now = _clock.UtcNow;

        if (tokenEntity.ExpiresAt <= now)
        {
            await _refreshTokenRepository.DeleteAsync(tokenEntity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<AuthResultDto>.Failure(ApplicationError.Unauthorized("AUTH_TOKEN_EXPIRED", "Refresh token has expired."));
        }

        if (tokenEntity.RevokedAt != null)
        {
            // If a revoked token is used, it might be a token theft attempt.
            // But for now, just reject it.
            return Result<AuthResultDto>.Failure(ApplicationError.Unauthorized("AUTH_TOKEN_REVOKED", "Refresh token has been revoked."));
        }

        var user = await _userRepository.GetByIdAsync(tokenEntity.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            return Result<AuthResultDto>.Failure(ApplicationError.Unauthorized("AUTH_INVALID_USER", "User is invalid or disabled."));
        }

        // Token rotation: delete old token and create new one
        await _refreshTokenRepository.DeleteAsync(tokenEntity, cancellationToken);

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newHashedRefreshToken = _tokenService.HashRefreshToken(newRefreshToken);

        var newTokenEntity = GodForge.Domain.Entities.Identity.RefreshToken.Create(
            user.Id,
            newHashedRefreshToken,
            null, // deviceName
            null, // ipAddress
            now.AddDays(7),
            now);

        await _refreshTokenRepository.AddAsync(newTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.Email, user.DisplayName, user.SystemRole.ToString());
        return Result<AuthResultDto>.Success(new AuthResultDto(newAccessToken, newRefreshToken, userDto));
    }
}
