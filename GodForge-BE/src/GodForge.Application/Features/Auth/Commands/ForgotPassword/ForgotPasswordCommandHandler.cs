using System.Net;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using GodForge.Domain.Entities.Identity;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<ChallengeAcceptedDto>>
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);
    private readonly IUserRepository _users;
    private readonly IAuthChallengeRepository _challenges;
    private readonly ISecretHashService _secretHash;
    private readonly ITokenService _tokens;
    private readonly IEmailOutbox _emailOutbox;
    private readonly IFrontendUrlBuilder _frontendUrlBuilder;
    private readonly IAuditWriter _auditWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ForgotPasswordCommandHandler(
        IUserRepository users,
        IAuthChallengeRepository challenges,
        ISecretHashService secretHash,
        ITokenService tokens,
        IEmailOutbox emailOutbox,
        IFrontendUrlBuilder frontendUrlBuilder,
        IAuditWriter auditWriter,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _challenges = challenges;
        _secretHash = secretHash;
        _tokens = tokens;
        _emailOutbox = emailOutbox;
        _frontendUrlBuilder = frontendUrlBuilder;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<ChallengeAcceptedDto>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
            return new ChallengeAcceptedDto(true, (int)Cooldown.TotalSeconds);

        var now = _clock.UtcNow;
        var challenge = await _challenges.GetActiveAsync(user.NormalizedEmail, AuthChallengePurposes.PasswordReset, cancellationToken);
        if (challenge is not null && challenge.IsInCooldown(now))
        {
            var remaining = Math.Max(1, (int)Math.Ceiling((challenge.ResendAvailableAt - now).TotalSeconds));
            return new ChallengeAcceptedDto(true, remaining);
        }

        var rawToken = _tokens.GenerateRefreshToken();
        var tokenHash = _secretHash.Hash(rawToken);
        if (challenge is null)
        {
            challenge = AuthChallenge.Create(user.NormalizedEmail, AuthChallengePurposes.PasswordReset, tokenHash, now, Lifetime, Cooldown);
            await _challenges.AddAsync(challenge, cancellationToken);
        }
        else
        {
            challenge.ReplaceSecret(tokenHash, now, Lifetime, Cooldown);
        }

        var resetLink = _frontendUrlBuilder.BuildPasswordResetUrl(user.Email, rawToken);
        var body = $"<h2>Password reset</h2><p>Hello {WebUtility.HtmlEncode(user.DisplayName)},</p><p><a href=\"{WebUtility.HtmlEncode(resetLink)}\">Reset your password</a>.</p><p>This link expires in 1 hour.</p>";
        await _emailOutbox.EnqueueAsync(user.Email, "GodForge - Password reset", body, request.CorrelationId, cancellationToken);
        await _auditWriter.WriteSecurityAsync(user.Id, "auth.password_reset_requested", "informational", null, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintConflictException exception) when (exception.ConstraintName == "ux_auth_challenges_active_scope")
        {
            _unitOfWork.ClearTrackedChanges();
            return new ChallengeAcceptedDto(true, (int)Cooldown.TotalSeconds);
        }
        catch (ConcurrencyConflictException)
        {
            _unitOfWork.ClearTrackedChanges();
            return new ChallengeAcceptedDto(true, (int)Cooldown.TotalSeconds);
        }
        return new ChallengeAcceptedDto(true, (int)Cooldown.TotalSeconds);
    }
}
