using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public LogoutCommandHandler(
        ICurrentUser currentUser,
        ITokenBlacklistService tokenBlacklistService,
        IRefreshTokenRepository refreshTokens,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _currentUser = currentUser;
        _tokenBlacklistService = tokenBlacklistService;
        _refreshTokens = refreshTokens;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Jti is not null && _currentUser.TokenExpiration is not null)
        {
            var expiresIn = _currentUser.TokenExpiration.Value - _clock.UtcNow;
            if (expiresIn > TimeSpan.Zero)
            {
                await _tokenBlacklistService.BlacklistTokenAsync(_currentUser.Jti, expiresIn, cancellationToken);
            }
        }

        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            var hashedToken = _tokenService.HashRefreshToken(request.RefreshToken);
            var tokenEntity = await _refreshTokens.GetByHashAsync(hashedToken, cancellationToken);
            if (tokenEntity is not null && tokenEntity.UserId == _currentUser.GetId())
            {
                await _refreshTokens.DeleteAsync(tokenEntity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        return Result.Success();
    }
}
