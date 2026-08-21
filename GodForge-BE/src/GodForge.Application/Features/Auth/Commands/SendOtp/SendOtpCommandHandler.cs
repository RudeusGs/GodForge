using System.Net;
using System.Security.Cryptography;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using GodForge.Domain.Entities.Identity;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.SendOtp;

public sealed class SendOtpCommandHandler : IRequestHandler<SendOtpCommand, Result<ChallengeAcceptedDto>>
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);
    private readonly IAuthChallengeRepository _challenges;
    private readonly ISecretHashService _secretHash;
    private readonly IEmailOutbox _emailOutbox;
    private readonly IAuditWriter _auditWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SendOtpCommandHandler(
        IAuthChallengeRepository challenges,
        ISecretHashService secretHash,
        IEmailOutbox emailOutbox,
        IAuditWriter auditWriter,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _challenges = challenges;
        _secretHash = secretHash;
        _emailOutbox = emailOutbox;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<ChallengeAcceptedDto>> Handle(SendOtpCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        var now = _clock.UtcNow;
        var normalizedEmail = User.NormalizeEmail(email);
        var challenge = await _challenges.GetActiveAsync(normalizedEmail, AuthChallengePurposes.Registration, cancellationToken);
        if (challenge is not null && challenge.IsInCooldown(now))
        {
            var remaining = Math.Max(1, (int)Math.Ceiling((challenge.ResendAvailableAt - now).TotalSeconds));
            return new ChallengeAcceptedDto(true, remaining);
        }

        var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var hash = _secretHash.Hash(otp);
        if (challenge is null)
        {
            challenge = AuthChallenge.Create(normalizedEmail, AuthChallengePurposes.Registration, hash, now, Lifetime, Cooldown);
            await _challenges.AddAsync(challenge, cancellationToken);
        }
        else
        {
            challenge.ReplaceSecret(hash, now, Lifetime, Cooldown);
        }

        var safeOtp = WebUtility.HtmlEncode(otp);
        var body = $"<h2>Verify your email address</h2><p>Your GodForge verification code is <strong>{safeOtp}</strong>.</p><p>This code expires in 5 minutes.</p>";
        await _emailOutbox.EnqueueAsync(email, "GodForge - Email verification", body, request.CorrelationId, cancellationToken);
        await _auditWriter.WriteSecurityAsync(
            null,
            "auth.registration_challenge_created",
            "informational",
            new { EmailHash = _secretHash.Hash(normalizedEmail) },
            cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintConflictException exception) when (exception.Constraint == UniqueConstraintKind.AuthChallengeActiveScope)
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
