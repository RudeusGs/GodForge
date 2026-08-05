using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Features.Auth.Commands.Login;
using GodForge.Domain.Entities.Identity;
using Moq;
using Xunit;

namespace GodForge.UnitTests.Application.Features.Auth.Commands.Login;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUserSessionRepository> _sessions = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IAuditWriter> _auditWriter = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _users.Object,
            _sessions.Object,
            _refreshTokens.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            _auditWriter.Object,
            _clock.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_GivenValidCredentials_CreatesSessionAndReturnsTokens()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new LoginCommand("test@example.com", "password123", "Firefox", "127.0.0.1", "unit-test");
        var user = User.Create("test@example.com", "Test User", "hashed_password", now);

        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _users.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.VerifyPassword(command.Password, user.PasswordHash)).Returns(true);
        _tokenService.SetupGet(x => x.RefreshTokenLifetime).Returns(TimeSpan.FromDays(30));
        _tokenService.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");
        _tokenService.Setup(x => x.HashRefreshToken("refresh_token")).Returns("hashed_refresh");
        _tokenService
            .Setup(x => x.GenerateAccessToken(user, It.IsAny<Guid>(), now))
            .Returns(new AccessTokenResult("access_token", now.AddMinutes(15)));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("access_token", result.Value!.AccessToken);
        Assert.Equal("refresh_token", result.Value.RefreshToken);
        Assert.Equal(now.AddDays(30), result.Value.RefreshTokenExpiresAt);
        Assert.Equal("test@example.com", result.Value.User.Email);
        Assert.Equal("Firefox", result.Value.Session.DeviceName);
        _sessions.Verify(x => x.AddAsync(It.Is<UserSession>(s => s.UserId == user.Id), It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokens.Verify(x => x.AddAsync(It.Is<RefreshToken>(t => t.UserId == user.Id), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GivenInvalidEmail_ReturnsUnauthorized()
    {
        var command = new LoginCommand("wrong@example.com", "password123", null, null, null);
        _users.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("AUTH_INVALID_CREDENTIALS", result.Error?.Code);
    }

    [Fact]
    public async Task Handle_GivenInvalidPassword_ReturnsUnauthorizedAndRecordsFailure()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new LoginCommand("test@example.com", "wrongpassword", null, null, null);
        var user = User.Create("test@example.com", "Test User", "hashed_password", now);

        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _users.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.VerifyPassword(command.Password, user.PasswordHash)).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("AUTH_INVALID_CREDENTIALS", result.Error?.Code);
        Assert.Equal(1, user.FailedLoginCount);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
