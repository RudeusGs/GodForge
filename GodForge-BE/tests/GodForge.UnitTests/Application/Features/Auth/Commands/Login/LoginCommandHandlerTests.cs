using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Features.Auth.Commands.Login;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using Moq;
using Xunit;


namespace GodForge.UnitTests.Application.Features.Auth.Commands.Login;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IClock> _mockClock;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockTokenService = new Mock<ITokenService>();
        _mockClock = new Mock<IClock>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _handler = new LoginCommandHandler(
            _mockUserRepository.Object,
            _mockRefreshTokenRepository.Object,
            _mockPasswordHasher.Object,
            _mockTokenService.Object,
            _mockClock.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_GivenValidCredentials_ReturnsTokensAndUserDto()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "password123");
        var now = DateTimeOffset.UtcNow;
        _mockClock.Setup(c => c.UtcNow).Returns(now);

        var user = User.Create("test@example.com", "Test User", "hashed_password", now);

        _mockUserRepository.Setup(x => x.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher.Setup(x => x.VerifyPassword("password123", "hashed_password"))
            .Returns(true);

        _mockTokenService.Setup(x => x.GenerateAccessToken(user)).Returns("access_token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");
        _mockTokenService.Setup(x => x.HashRefreshToken("refresh_token")).Returns("hashed_refresh");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("access_token", result.Value!.AccessToken);
        Assert.Equal("refresh_token", result.Value.RefreshToken);
        Assert.Equal("test@example.com", result.Value.User.Email);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GivenInvalidEmail_ReturnsUnauthorized()
    {
        // Arrange
        var command = new LoginCommand("wrong@example.com", "password123");
        _mockUserRepository.Setup(x => x.GetByEmailAsync("wrong@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("AUTH_INVALID_CREDENTIALS", result.Error.Code);
    }

    [Fact]
    public async Task Handle_GivenInvalidPassword_ReturnsUnauthorizedAndRecordsFailure()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "wrongpassword");
        var now = DateTimeOffset.UtcNow;
        _mockClock.Setup(c => c.UtcNow).Returns(now);

        var user = User.Create("test@example.com", "Test User", "hashed_password", now);

        _mockUserRepository.Setup(x => x.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher.Setup(x => x.VerifyPassword("wrongpassword", "hashed_password"))
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("AUTH_INVALID_CREDENTIALS", result.Error.Code);
        Assert.Equal(1, user.FailedLoginCount);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
