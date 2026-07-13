using System.Linq;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.Commands.SendRegisterOtp;
using GodForge.Domain.Entities.Identity;
using Moq;
using Xunit;

namespace GodForge.UnitTests.Application.Features.Auth.Commands.SendRegisterOtp;

public class SendRegisterOtpCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly SendRegisterOtpCommandHandler _handler;

    public SendRegisterOtpCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockCacheService = new Mock<ICacheService>();
        _mockEmailService = new Mock<IEmailService>();

        _handler = new SendRegisterOtpCommandHandler(
            _mockUserRepository.Object,
            _mockCacheService.Object,
            _mockEmailService.Object);
    }

    [Fact]
    public async Task Handle_GivenNewEmail_GeneratesOtpSavesToCacheAndSendsEmail()
    {
        // Arrange
        var command = new SendRegisterOtpCommand("newuser@example.com");

        _mockUserRepository.Setup(x => x.GetByEmailAsync("newuser@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _mockCacheService.Verify(c => c.SetAsync(
            "otp:register:newuser@example.com",
            It.Is<string>(otp => otp.Length == 6 && otp.All(char.IsDigit)),
            TimeSpan.FromMinutes(5),
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockEmailService.Verify(e => e.SendEmailAsync(
            "newuser@example.com",
            It.Is<string>(s => s.Contains("Verification")),
            It.Is<string>(b => b.Contains("Verify your email address")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GivenExistingEmail_ReturnsConflictError()
    {
        // Arrange
        var command = new SendRegisterOtpCommand("existing@example.com");
        var now = DateTimeOffset.UtcNow;
        var existingUser = User.Create("existing@example.com", "Existing", "hash", now);

        _mockUserRepository.Setup(x => x.GetByEmailAsync("existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("AUTH_EMAIL_EXISTS", result.Error.Code);

        _mockCacheService.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<TimeSpan>(),
            null,
            It.IsAny<CancellationToken>()), Times.Never);

        _mockEmailService.Verify(e => e.SendEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
