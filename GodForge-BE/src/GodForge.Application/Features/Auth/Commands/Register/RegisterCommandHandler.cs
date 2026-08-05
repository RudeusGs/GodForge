using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using GodForge.Domain.Entities.Identity;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<UserDto>>
{
    private readonly IUserRepository _users;
    private readonly IAuthChallengeRepository _challenges;
    private readonly ISecretHashService _secretHash;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(
        IUserRepository users,
        IAuthChallengeRepository challenges,
        ISecretHashService secretHash,
        IPasswordHasher passwordHasher,
        IAuditWriter auditWriter,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _challenges = challenges;
        _secretHash = secretHash;
        _passwordHasher = passwordHasher;
        _auditWriter = auditWriter;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var normalizedEmail = User.NormalizeEmail(request.Email);
        var challenge = await _challenges.GetActiveAsync(normalizedEmail, AuthChallengePurposes.Registration, cancellationToken);
        if (challenge is null || now >= challenge.ExpiresAt)
            return ApplicationError.Validation("AUTH_OTP_EXPIRED", "OTP expired or not found. Please request a new one.");

        if (!_secretHash.Verify(request.Otp, challenge.SecretHash))
        {
            challenge.RecordFailedAttempt(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationError.Validation("AUTH_OTP_INVALID", "Invalid OTP verification code.");
        }

        if (await _users.GetByEmailAsync(request.Email, cancellationToken) is not null)
            return ApplicationError.Conflict("AUTH_EMAIL_EXISTS", "Email is already in use.");

        var user = User.Create(request.Email, request.DisplayName, _passwordHasher.HashPassword(request.Password), now);
        user.MarkEmailVerified(now);
        challenge.Consume(now);
        await _users.AddAsync(user, cancellationToken);
        await _auditWriter.WriteSecurityAsync(user.Id, "auth.registration_completed", "informational", new { user.Email }, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return ApplicationError.Validation("AUTH_OTP_INVALID", "The OTP has already been consumed.");
        }
        catch (UniqueConstraintConflictException exception)
            when (exception.ConstraintName == "ux_users_normalized_email")
        {
            return ApplicationError.Conflict("AUTH_EMAIL_EXISTS", "Email is already in use.");
        }

        return UserDto.From(user);
    }
}
