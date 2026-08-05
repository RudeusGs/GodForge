using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly ITokenBlacklistService _tokenBlacklist;
    private readonly IUserSessionRepository _sessions;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IAuditWriter _auditWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public LogoutCommandHandler(
        ICurrentUser currentUser,
        ITokenBlacklistService tokenBlacklist,
        IUserSessionRepository sessions,
        IRefreshTokenRepository refreshTokens,
        IAuditWriter auditWriter,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _currentUser = currentUser;
        _tokenBlacklist = tokenBlacklist;
        _sessions = sessions;
        _refreshTokens = refreshTokens;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var userId = _currentUser.GetId();
        var sessionId = _currentUser.GetSessionId();
        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is not null && session.UserId == userId)
            session.Revoke("logout", now);
        await _refreshTokens.RevokeAllForSessionAsync(sessionId, "logout", now, cancellationToken);

        if (_currentUser.Jti is not null && _currentUser.TokenExpiration is { } expiration)
        {
            var remaining = expiration - now;
            if (remaining > TimeSpan.Zero)
                await _tokenBlacklist.BlacklistTokenAsync(_currentUser.Jti, remaining, cancellationToken);
        }

        await _auditWriter.WriteSecurityAsync(userId, "auth.logout", "informational", new { SessionId = sessionId }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
