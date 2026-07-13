using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.Commands.Register;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using Moq;
using Xunit;

namespace GodForge.UnitTests.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IClock> _mockClock;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockTokenService = new Mock<ITokenService>();
        _mockClock = new Mock<IClock>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCacheService = new Mock<ICacheService>();

        _handler = new RegisterCommandHandler(
            _mockUserRepository.Object,
            _mockRefreshTokenRepository.Object,
            _mockPasswordHasher.Object,
            _mockTokenService.Object,
            _mockClock.Object,
            _mockUnitOfWork.Object,
            _mockCacheService.Object);
    }

    [Fact]
    public async Task Handle_GivenValidDataAndValidOtp_CreatesUserAndReturnsTokens()
    {
        // Arrange
        var command = new RegisterCommand("newuser@example.com", "New User", "password123", "123456");
        var now = DateTimeOffset.UtcNow;
        _mockClock.Setup(c => c.UtcNow).Returns(now);

        _mockCacheService.Setup(c => c.GetAsync<string>("otp:register:newuser@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456");

        _mockUserRepository.Setup(x => x.GetByEmailAsync("newuser@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _mockPasswordHasher.Setup(x => x.HashPassword("password123"))
            .Returns("hashed_password");

        _mockTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access_token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");
        _mockTokenService.Setup(x => x.HashRefreshToken("refresh_token")).Returns("hashed_refresh");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("access_token", result.Value!.AccessToken);
        Assert.Equal("refresh_token", result.Value.RefreshToken);
        Assert.Equal("newuser@example.com", result.Value.User.Email);

        _mockUserRepository.Verify(x => x.AddAsync(It.Is<User>(u => u.Email == "newuser@example.com" && u.PasswordHash == "hashed_password"), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.RemoveAsync("otp:register:newuser@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GivenExpiredOtp_ReturnsValidationError()
    {
        // Arrange
        var command = new RegisterCommand("newuser@example.com", "New User", "password123", "123456");
        
        _mockCacheService.Setup(c => c.GetAsync<string>("otp:register:newuser@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("AUTH_OTP_EXPIRED", result.Error.Code);
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GivenInvalidOtp_ReturnsValidationError()
    {
        // Arrange
        var command = new RegisterCommand("newuser@example.com", "New User", "password123", "123456");
        
        _mockCacheService.Setup(c => c.GetAsync<string>("otp:register:newuser@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync("654321"); // Mismatch

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("AUTH_OTP_INVALID", result.Error.Code);
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GivenExistingEmail_ReturnsConflictError()
    {
        // Arrange
        var command = new RegisterCommand("existing@example.com", "Existing User", "password123", "123456");
        var now = DateTimeOffset.UtcNow;
        _mockClock.Setup(c => c.UtcNow).Returns(now);

        _mockCacheService.Setup(c => c.GetAsync<string>("otp:register:existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456");

        var existingUser = User.Create("existing@example.com", "Existing User", "hash", now);

        _mockUserRepository.Setup(x => x.GetByEmailAsync("existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("AUTH_EMAIL_EXISTS", result.Error.Code);

        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
