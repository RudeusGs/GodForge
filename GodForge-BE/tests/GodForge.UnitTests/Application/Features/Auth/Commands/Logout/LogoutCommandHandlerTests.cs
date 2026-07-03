using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.Commands.Logout;
using GodForge.Domain.Entities.Identity;
using Moq;
using Xunit;

namespace GodForge.UnitTests.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandlerTests
{
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly Mock<ITokenBlacklistService> _mockTokenBlacklistService;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IClock> _mockClock;
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _mockCurrentUser = new Mock<ICurrentUser>();
        _mockTokenBlacklistService = new Mock<ITokenBlacklistService>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _mockTokenService = new Mock<ITokenService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockClock = new Mock<IClock>();

        _handler = new LogoutCommandHandler(
            _mockCurrentUser.Object,
            _mockTokenBlacklistService.Object,
            _mockRefreshTokenRepository.Object,
            _mockTokenService.Object,
            _mockUnitOfWork.Object,
            _mockClock.Object);
    }

    [Fact]
    public async Task Handle_WhenJtiExists_AddsToBlacklist()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var exp = now.AddMinutes(10);
        var command = new LogoutCommand(null);

        _mockClock.Setup(c => c.UtcNow).Returns(now);
        _mockCurrentUser.Setup(c => c.Jti).Returns(jti);
        _mockCurrentUser.Setup(c => c.TokenExpiration).Returns(exp);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockTokenBlacklistService.Verify(x => x.BlacklistTokenAsync(jti, It.Is<TimeSpan>(t => t.TotalMinutes > 9 && t.TotalMinutes <= 10), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenProvided_DeletesFromDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new LogoutCommand("my-refresh-token");
        var now = DateTimeOffset.UtcNow;

        _mockClock.Setup(c => c.UtcNow).Returns(now);
        _mockCurrentUser.Setup(c => c.GetId()).Returns(userId);

        _mockTokenService.Setup(x => x.HashRefreshToken("my-refresh-token"))
            .Returns("hashed-token");

        var tokenEntity = RefreshToken.Create(userId, "hashed-token", null, null, now.AddDays(1), now);

        _mockRefreshTokenRepository.Setup(x => x.GetByHashAsync("hashed-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenEntity);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockRefreshTokenRepository.Verify(x => x.DeleteAsync(tokenEntity, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
