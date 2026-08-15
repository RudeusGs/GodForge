using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResultDto>>
{
    private readonly IUserRepository _users;
    private readonly IUserSessionRepository _sessions;
    private readonly ISessionValidationService _sessionValidation;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenService _tokenService;
    private readonly IAuditWriter _auditWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RefreshTokenCommandHandler(
        IUserRepository users,
        IUserSessionRepository sessions,
        ISessionValidationService sessionValidation,
        IRefreshTokenRepository refreshTokens,
        ITokenService tokenService,
        IAuditWriter auditWriter,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _sessions = sessions;
        _sessionValidation = sessionValidation;
        _refreshTokens = refreshTokens;
        _tokenService = tokenService;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<AuthResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = _tokenService.HashRefreshToken(request.RefreshToken);
        var token = await _refreshTokens.GetByHashAsync(hash, cancellationToken);
        if (token is null)
            return ApplicationError.Unauthorized("AUTH_TOKEN_REVOKED", "Refresh token is invalid or revoked.");

        var now = _clock.UtcNow;
        if (token.ExpiresAt <= now)
        {
            token.Revoke(now, "expired");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationError.Unauthorized("AUTH_TOKEN_EXPIRED", "Refresh token has expired.");
        }

        if (token.RevokedAt is not null)
        {
            await RevokeCompromisedScopeAsync(token, now, cancellationToken);
            return ApplicationError.Unauthorized("AUTH_REFRESH_REUSED", "Refresh token reuse was detected.");
        }

        var user = await _users.GetByIdAsync(token.UserId, cancellationToken);
        var session = await _sessions.GetActiveAsync(token.SessionId, token.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active || session is null || !session.IsActive(now))
            return ApplicationError.Unauthorized("AUTH_TOKEN_REVOKED", "The user session is no longer active.");

        var rawReplacement = _tokenService.GenerateRefreshToken();
        var replacementHash = _tokenService.HashRefreshToken(rawReplacement);
        token.Revoke(now, "rotated", replacementHash);
        var replacement = GodForge.Domain.Entities.Identity.RefreshToken.Create(
            user.Id,
            session.Id,
            token.FamilyId,
            replacementHash,
            session.ExpiresAt,
            now);
        session.RecordActivity(now);
        var access = _tokenService.GenerateAccessToken(user, session.Id, now);
        await _refreshTokens.AddAsync(replacement, cancellationToken);
        await _auditWriter.WriteSecurityAsync(user.Id, "auth.refresh_rotated", "informational", new { SessionId = session.Id, FamilyId = token.FamilyId }, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _unitOfWork.ClearTrackedChanges();
            var latest = await _refreshTokens.GetByHashAsync(hash, cancellationToken);
            if (latest is not null)
                await RevokeCompromisedScopeAsync(latest, now, cancellationToken);
            return ApplicationError.Unauthorized("AUTH_REFRESH_REUSED", "Refresh token reuse was detected.");
        }

        return new AuthResultDto(
            UserDto.From(user),
            SessionDto.From(session, session.Id),
            access.Token,
            access.ExpiresAt,
            rawReplacement,
            replacement.ExpiresAt);
    }

    private async Task RevokeCompromisedScopeAsync(GodForge.Domain.Entities.Identity.RefreshToken token, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await _refreshTokens.RevokeAllForFamilyAsync(token.FamilyId, "refresh-token-reuse", now, cancellationToken);
        var session = await _sessions.GetByIdAsync(token.SessionId, cancellationToken);
        session?.Revoke("refresh-token-reuse", now);
        await _auditWriter.WriteSecurityAsync(token.UserId, "auth.refresh_reuse_detected", "critical", new { token.SessionId, token.FamilyId }, cancellationToken);
        await _sessionValidation.InvalidateSessionAsync(token.SessionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
