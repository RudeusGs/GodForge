using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Features.Auth;
using GodForge.Application.Features.Auth.Commands.ResetPassword;
using GodForge.Domain.Entities.Identity;
using Moq;

namespace GodForge.UnitTests.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IAuthChallengeRepository> _challenges = new();
    private readonly Mock<ISecretHashService> _secretHash = new();
    private readonly Mock<IPasswordHasher> _passwords = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IUserSessionRepository> _sessions = new();
    private readonly Mock<ISessionValidationService> _sessionValidation = new();
    private readonly Mock<IAuditWriter> _audit = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();

    [Fact]
    public async Task Handle_ValidChallenge_ChangesPasswordConsumesChallengeAndRevokesSessions()
    {
        var now = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var user = User.Create("reset@example.com", "Reset User", "old-hash", now.AddDays(-1));
        var challenge = AuthChallenge.Create(
            user.NormalizedEmail,
            AuthChallengePurposes.PasswordReset,
            "token-hash",
            now.AddMinutes(-1),
            TimeSpan.FromHours(1),
            TimeSpan.FromSeconds(60));
        var originalSecurityStamp = user.SecurityStamp;
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _users.Setup(x => x.GetByEmailAsync("reset@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _challenges.Setup(x => x.GetActiveAsync(
            user.NormalizedEmail,
            AuthChallengePurposes.PasswordReset,
            It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        _secretHash.Setup(x => x.Verify("raw-token", "token-hash")).Returns(true);
        _passwords.Setup(x => x.HashPassword("NewPassword1")).Returns("new-hash");
        var revokedSessionIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        _sessions.Setup(x => x.RevokeAllForUserAsync(user.Id, "password-reset", now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedSessionIds);
        var sequence = new MockSequence();
        _sessionValidation.InSequence(sequence)
            .Setup(x => x.InvalidateSessionsAsync(revokedSessionIds, CancellationToken.None))
            .Returns(Task.CompletedTask);
        _unitOfWork.InSequence(sequence)
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ReturnsAsync(1);

        var result = await CreateHandler().Handle(
            new ResetPasswordCommand("reset@example.com", "raw-token", "NewPassword1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("new-hash", user.PasswordHash);
        Assert.NotEqual(originalSecurityStamp, user.SecurityStamp);
        Assert.NotNull(challenge.ConsumedAt);
        _refreshTokens.Verify(x => x.RevokeAllForUserAsync(
            user.Id, "password-reset", now, It.IsAny<CancellationToken>()), Times.Once);
        _sessions.Verify(x => x.RevokeAllForUserAsync(
            user.Id, "password-reset", now, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sessionValidation.Verify(x => x.InvalidateSessionsAsync(revokedSessionIds, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidToken_RecordsAttemptWithoutChangingPassword()
    {
        var now = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var user = User.Create("reset@example.com", "Reset User", "old-hash", now.AddDays(-1));
        var challenge = AuthChallenge.Create(
            user.NormalizedEmail,
            AuthChallengePurposes.PasswordReset,
            "token-hash",
            now.AddMinutes(-1),
            TimeSpan.FromHours(1),
            TimeSpan.FromSeconds(60));
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _users.Setup(x => x.GetByEmailAsync("reset@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _challenges.Setup(x => x.GetActiveAsync(
            user.NormalizedEmail,
            AuthChallengePurposes.PasswordReset,
            It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        _secretHash.Setup(x => x.Verify("wrong-token", "token-hash")).Returns(false);

        var result = await CreateHandler().Handle(
            new ResetPasswordCommand("reset@example.com", "wrong-token", "NewPassword1"),
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("AUTH_RESET_TOKEN_INVALID", result.Error!.Code);
        Assert.Equal("old-hash", user.PasswordHash);
        Assert.Equal(1, challenge.FailedAttempts);
        _sessions.Verify(x => x.RevokeAllForUserAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private ResetPasswordCommandHandler CreateHandler() => new(
        _users.Object,
        _challenges.Object,
        _secretHash.Object,
        _passwords.Object,
        _refreshTokens.Object,
        _sessions.Object,
        _sessionValidation.Object,
        _audit.Object,
        _unitOfWork.Object,
        _clock.Object);
}
