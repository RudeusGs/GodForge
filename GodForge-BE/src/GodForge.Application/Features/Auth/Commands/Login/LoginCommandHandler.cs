using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResultDto>>
{
    private readonly IUserRepository _users;
    private readonly IUserSessionRepository _sessions;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ISecretHashService _secretHash;
    private readonly IAuditWriter _auditWriter;
    private readonly IM1QuotaPolicy _quotaPolicy;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IUserRepository users,
        IUserSessionRepository sessions,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ISecretHashService secretHash,
        IAuditWriter auditWriter,
        IM1QuotaPolicy quotaPolicy,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _sessions = sessions;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _secretHash = secretHash;
        _auditWriter = auditWriter;
        _quotaPolicy = quotaPolicy;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            // Keep the invalid-email path computationally comparable to a failed password check.
            _ = _passwordHasher.HashPassword(request.Password);
            await _auditWriter.WriteSecurityAsync(null, "auth.login_failed", "medium", new { Reason = "invalid-credentials" }, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationError.Unauthorized("AUTH_INVALID_CREDENTIALS", "Invalid email or password.");
        }
        // Do the expensive verification before taking the user lock, but make no account
        // decision from this potentially stale aggregate. Every known-user login outcome,
        // including failure counters and lockout, is serialized and re-evaluated below.
        _ = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.AcquireResourceLockAsync("user-active-sessions", user.Id, cancellationToken);
            _unitOfWork.ClearTrackedChanges();
            user = await _users.GetByIdAsync(user.Id, cancellationToken);
            if (user is null)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApplicationError.Unauthorized("AUTH_INVALID_CREDENTIALS", "Invalid email or password.");
            }

            // Re-verify after the lock/reload so a concurrent password reset or status change
            // cannot create a session from stale credentials.
            var passwordIsValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
            if (user.Status == UserStatus.Locked && user.LockedUntil > now)
            {
                await _auditWriter.WriteSecurityAsync(user.Id, "auth.login_blocked", "high", new { Reason = "account-locked" }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return ApplicationError.Unauthorized("AUTH_INVALID_CREDENTIALS", "Invalid email or password.");
            }
            if (user.Status == UserStatus.Locked)
                user.Unlock(now);
            if (user.Status != UserStatus.Active)
            {
                var reason = user.Status == UserStatus.Disabled ? "account-disabled" : "invalid-credentials";
                await _auditWriter.WriteSecurityAsync(user.Id, "auth.login_blocked", "high", new { Reason = reason }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return ApplicationError.Unauthorized("AUTH_INVALID_CREDENTIALS", "Invalid email or password.");
            }
            if (!passwordIsValid)
            {
                user.RecordLoginFailure(now, 5, TimeSpan.FromMinutes(15));
                await _auditWriter.WriteSecurityAsync(user.Id, "auth.login_failed", "medium", new { Reason = "invalid-credentials", user.FailedLoginCount }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return ApplicationError.Unauthorized("AUTH_INVALID_CREDENTIALS", "Invalid email or password.");
            }

            var activeSessionCount = await _sessions.CountActiveForUserAsync(user.Id, now, cancellationToken);
            if (activeSessionCount >= _quotaPolicy.MaxActiveSessionsPerUser)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApplicationError.Conflict(
                    "AUTH_SESSION_LIMIT_REACHED",
                    "The active session limit has been reached. Revoke an existing session and try again.");
            }

            user.RecordLoginSuccess(now);
            var refreshExpiresAt = now.Add(_tokenService.RefreshTokenLifetime);
            var session = UserSession.Create(
                user.Id,
                request.DeviceName,
                HashOptional(request.IpAddress),
                HashOptional(request.UserAgent),
                refreshExpiresAt,
                now);
            var rawRefreshToken = _tokenService.GenerateRefreshToken();
            var refreshToken = GodForge.Domain.Entities.Identity.RefreshToken.Create(
                user.Id,
                session.Id,
                Guid.NewGuid(),
                _tokenService.HashRefreshToken(rawRefreshToken),
                refreshExpiresAt,
                now);
            var access = _tokenService.GenerateAccessToken(user, session.Id, now);

            await _sessions.AddAsync(session, cancellationToken);
            await _refreshTokens.AddAsync(refreshToken, cancellationToken);
            await _auditWriter.WriteSecurityAsync(user.Id, "auth.login_succeeded", "informational", new { SessionId = session.Id, request.DeviceName }, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new AuthResultDto(
                UserDto.From(user),
                SessionDto.From(session, session.Id),
                access.Token,
                access.ExpiresAt,
                rawRefreshToken,
                refreshExpiresAt);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    private string? HashOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : _secretHash.Hash(value.Trim());
}
