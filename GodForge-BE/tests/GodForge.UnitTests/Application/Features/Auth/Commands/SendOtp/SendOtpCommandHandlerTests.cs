using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Features.Auth;
using GodForge.Application.Features.Auth.Commands.SendOtp;
using GodForge.Domain.Entities.Identity;
using Moq;
using Xunit;

namespace GodForge.UnitTests.Application.Features.Auth.Commands.SendOtp;

public class SendOtpCommandHandlerTests
{
    private readonly Mock<IAuthChallengeRepository> _challenges = new();
    private readonly Mock<ISecretHashService> _secretHash = new();
    private readonly Mock<IEmailOutbox> _emailOutbox = new();
    private readonly Mock<IAuditWriter> _auditWriter = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();
    private readonly SendOtpCommandHandler _handler;

    public SendOtpCommandHandlerTests()
    {
        _handler = new SendOtpCommandHandler(
            _challenges.Object,
            _secretHash.Object,
            _emailOutbox.Object,
            _auditWriter.Object,
            _unitOfWork.Object,
            _clock.Object);
    }

    [Fact]
    public async Task Handle_GivenNewEmail_PersistsHashedChallengeAndQueuesEmail()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new SendOtpCommand("newuser@example.com", "correlation-id");

        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _challenges.Setup(x => x.GetActiveAsync(User.NormalizeEmail(command.Email), AuthChallengePurposes.Registration, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthChallenge?)null);
        _secretHash.Setup(x => x.Hash(It.Is<string>(otp => otp.Length == 6 && otp.All(char.IsDigit)))).Returns("hashed-otp");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(60, result.Value?.ResendAfterSeconds);
        _challenges.Verify(x => x.AddAsync(
            It.Is<AuthChallenge>(c => c.NormalizedEmail == User.NormalizeEmail(command.Email) && c.SecretHash == "hashed-otp"),
            It.IsAny<CancellationToken>()), Times.Once);
        _emailOutbox.Verify(x => x.EnqueueAsync(
            command.Email,
            It.Is<string>(s => s.Contains("verification", StringComparison.OrdinalIgnoreCase)),
            It.Is<string>(body => body.Contains("Verify your email address", StringComparison.Ordinal)),
            command.CorrelationId,
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }



    [Fact]
    public async Task Handle_DuringCooldown_ReturnsAcceptedWithoutSendingAnotherMessage()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new SendOtpCommand("newuser@example.com", "correlation-id");
        var challenge = AuthChallenge.Create(User.NormalizeEmail(command.Email), AuthChallengePurposes.Registration, "hash", now, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(60));

        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _challenges.Setup(x => x.GetActiveAsync(User.NormalizeEmail(command.Email), AuthChallengePurposes.Registration, It.IsAny<CancellationToken>())).ReturnsAsync(challenge);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value?.ResendAfterSeconds > 0);
        _emailOutbox.Verify(x => x.EnqueueAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
