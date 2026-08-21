using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Features.Auth;
using GodForge.Application.Features.Auth.Commands.Register;
using GodForge.Domain.Entities.Identity;
using Moq;
using Xunit;

namespace GodForge.UnitTests.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IAuthChallengeRepository> _challenges = new();
    private readonly Mock<ISecretHashService> _secretHash = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAuditWriter> _auditWriter = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _handler = new RegisterCommandHandler(
            _users.Object,
            _challenges.Object,
            _secretHash.Object,
            _passwordHasher.Object,
            _auditWriter.Object,
            _clock.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_GivenValidDataAndOtp_CreatesVerifiedUserAndConsumesChallenge()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new RegisterCommand("newuser@example.com", "New User", "password123", "123456");
        var challenge = ActiveChallenge(command.Email, now);

        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _challenges.Setup(x => x.GetActiveAsync(User.NormalizeEmail(command.Email), AuthChallengePurposes.Registration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);
        _secretHash.Setup(x => x.Verify(command.Otp, challenge.SecretHash)).Returns(true);
        _users.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _passwordHasher.Setup(x => x.HashPassword(command.Password)).Returns("hashed_password");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(command.Email, result.Value?.Email);
        Assert.NotNull(result.Value?.EmailVerifiedAt);
        Assert.NotNull(challenge.ConsumedAt);
        _users.Verify(x => x.AddAsync(
            It.Is<User>(u => u.Email == command.Email && u.PasswordHash == "hashed_password"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GivenMissingChallenge_ReturnsOtpExpired()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new RegisterCommand("newuser@example.com", "New User", "password123", "123456");
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _challenges.Setup(x => x.GetActiveAsync(User.NormalizeEmail(command.Email), AuthChallengePurposes.Registration, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthChallenge?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("AUTH_OTP_EXPIRED", result.Error?.Code);
        _users.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GivenInvalidOtp_RecordsAttemptAndReturnsValidationError()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new RegisterCommand("newuser@example.com", "New User", "password123", "123456");
        var challenge = ActiveChallenge(command.Email, now);

        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _challenges.Setup(x => x.GetActiveAsync(User.NormalizeEmail(command.Email), AuthChallengePurposes.Registration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);
        _secretHash.Setup(x => x.Verify(command.Otp, challenge.SecretHash)).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("AUTH_OTP_INVALID", result.Error?.Code);
        Assert.Equal(1, challenge.FailedAttempts);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AttemptExhaustedChallenge_ReturnsSafeOtpErrorWithoutCreatingUser()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new RegisterCommand("newuser@example.com", "New User", "password123", "123456");
        var challenge = ActiveChallenge(command.Email, now);
        for (var attempt = 0; attempt < challenge.MaxAttempts; attempt++)
            challenge.RecordFailedAttempt(now);

        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _challenges.Setup(x => x.GetActiveAsync(
                User.NormalizeEmail(command.Email),
                AuthChallengePurposes.Registration,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);
        _secretHash.Setup(x => x.Verify(command.Otp, challenge.SecretHash)).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("AUTH_OTP_INVALID", result.Error?.Code);
        _users.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GivenExistingEmail_ReturnsConflict()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new RegisterCommand("existing@example.com", "Existing User", "password123", "123456");
        var challenge = ActiveChallenge(command.Email, now);
        var existing = User.Create(command.Email, command.DisplayName, "hash", now);

        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _challenges.Setup(x => x.GetActiveAsync(User.NormalizeEmail(command.Email), AuthChallengePurposes.Registration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);
        _secretHash.Setup(x => x.Verify(command.Otp, challenge.SecretHash)).Returns(true);
        _users.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("AUTH_EMAIL_EXISTS", result.Error?.Code);
        _users.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUniqueEmailConstraintWinsRace_ReturnsConflict()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new RegisterCommand("race@example.com", "Race User", "password123", "123456");
        var challenge = ActiveChallenge(command.Email, now);

        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _challenges.Setup(x => x.GetActiveAsync(User.NormalizeEmail(command.Email), AuthChallengePurposes.Registration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);
        _secretHash.Setup(x => x.Verify(command.Otp, challenge.SecretHash)).Returns(true);
        _users.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _passwordHasher.Setup(x => x.HashPassword(command.Password)).Returns("hashed_password");
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GodForge.Application.Common.Models.UniqueConstraintConflictException(
                "duplicate email",
                GodForge.Application.Common.Models.UniqueConstraintKind.UserNormalizedEmail));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("AUTH_EMAIL_EXISTS", result.Error?.Code);
    }

    private static AuthChallenge ActiveChallenge(string email, DateTimeOffset now)
        => AuthChallenge.Create(
            User.NormalizeEmail(email),
            AuthChallengePurposes.Registration,
            "otp_hash",
            now,
            TimeSpan.FromMinutes(5),
            TimeSpan.Zero);
}
