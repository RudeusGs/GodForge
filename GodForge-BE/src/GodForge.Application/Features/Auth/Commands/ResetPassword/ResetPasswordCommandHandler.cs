using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IUserRepository _users;
    private readonly IAuthChallengeRepository _challenges;
    private readonly ISecretHashService _secretHash;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserSessionRepository _sessions;
    private readonly IAuditWriter _auditWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ResetPasswordCommandHandler(
        IUserRepository users,
        IAuthChallengeRepository challenges,
        ISecretHashService secretHash,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokens,
        IUserSessionRepository sessions,
        IAuditWriter auditWriter,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _challenges = challenges;
        _secretHash = secretHash;
        _passwordHasher = passwordHasher;
        _refreshTokens = refreshTokens;
        _sessions = sessions;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
            return InvalidToken();

        var challenge = await _challenges.GetActiveAsync(user.NormalizedEmail, AuthChallengePurposes.PasswordReset, cancellationToken);
        if (challenge is null || !challenge.IsActive(now))
            return InvalidToken();

        if (!_secretHash.Verify(request.Token, challenge.SecretHash))
        {
            challenge.RecordFailedAttempt(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return InvalidToken();
        }

        challenge.Consume(now);
        user.UpdatePassword(_passwordHasher.HashPassword(request.NewPassword), now);
        user.ClearPasswordResetToken();
        await _refreshTokens.RevokeAllForUserAsync(user.Id, "password-reset", now, cancellationToken);
        await _sessions.RevokeAllForUserAsync(user.Id, "password-reset", now, cancellationToken);
        await _auditWriter.WriteSecurityAsync(user.Id, "auth.password_reset", "high", new { SessionsRevoked = true }, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return InvalidToken();
        }

        return Result.Success();
    }

    private static Result InvalidToken()
        => Result.Failure(ApplicationError.Validation("AUTH_RESET_TOKEN_INVALID", "Invalid or expired reset token."));
}
