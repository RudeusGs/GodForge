using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.Commands.RefreshToken;
using GodForge.Domain.Entities.Identity;
using Moq;

namespace GodForge.UnitTests.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUserSessionRepository> _sessions = new();
    private readonly Mock<ISessionValidationService> _sessionValidation = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IAuditWriter> _audit = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();

    [Fact]
    public async Task Handle_ActiveToken_RotatesOnceAndReturnsReplacement()
    {
        var fixture = CreateFixture();
        ConfigureActiveFixture(fixture);
        _tokens.Setup(x => x.GenerateRefreshToken()).Returns("replacement");
        _tokens.Setup(x => x.HashRefreshToken("replacement")).Returns("replacement-hash");
        _tokens.Setup(x => x.GenerateAccessToken(fixture.User, fixture.Session.Id, fixture.Now))
            .Returns(new AccessTokenResult("access", fixture.Now.AddMinutes(15)));

        var result = await CreateHandler().Handle(new RefreshTokenCommand("presented"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("replacement", result.Value.RefreshToken);
        Assert.NotNull(fixture.Token.RevokedAt);
        _refreshTokens.Verify(x => x.AddAsync(
            It.Is<GodForge.Domain.Entities.Identity.RefreshToken>(token =>
                token.FamilyId == fixture.Token.FamilyId && token.TokenHash == "replacement-hash"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReplayedToken_RevokesFamilyAndSession()
    {
        var fixture = CreateFixture();
        fixture.Token.Revoke(fixture.Now.AddMinutes(-1), "rotated", "replacement-hash");
        _clock.SetupGet(x => x.UtcNow).Returns(fixture.Now);
        _tokens.Setup(x => x.HashRefreshToken("presented")).Returns("presented-hash");
        _refreshTokens.Setup(x => x.GetByHashAsync("presented-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fixture.Token);
        _sessions.Setup(x => x.GetByIdAsync(fixture.Session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fixture.Session);
        var sequence = new MockSequence();
        _sessionValidation.InSequence(sequence)
            .Setup(x => x.InvalidateSessionAsync(fixture.Session.Id, CancellationToken.None))
            .Returns(Task.CompletedTask);
        _unitOfWork.InSequence(sequence)
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ReturnsAsync(1);

        var result = await CreateHandler().Handle(new RefreshTokenCommand("presented"), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("AUTH_REFRESH_REUSED", result.Error!.Code);
        Assert.NotNull(fixture.Session.RevokedAt);
        _sessionValidation.Verify(x => x.InvalidateSessionAsync(fixture.Session.Id, CancellationToken.None), Times.Once);
        _refreshTokens.Verify(x => x.RevokeAllForFamilyAsync(
            fixture.Token.FamilyId,
            "refresh-token-reuse",
            fixture.Now,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ConcurrentRotationConflict_IsTreatedAsReplay()
    {
        var fixture = CreateFixture();
        ConfigureActiveFixture(fixture);
        _tokens.Setup(x => x.GenerateRefreshToken()).Returns("replacement");
        _tokens.Setup(x => x.HashRefreshToken("replacement")).Returns("replacement-hash");
        _tokens.Setup(x => x.GenerateAccessToken(fixture.User, fixture.Session.Id, fixture.Now))
            .Returns(new AccessTokenResult("access", fixture.Now.AddMinutes(15)));
        _unitOfWork.SetupSequence(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("refresh conflict"))
            .ReturnsAsync(1);
        _sessions.Setup(x => x.GetByIdAsync(fixture.Session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fixture.Session);

        var result = await CreateHandler().Handle(new RefreshTokenCommand("presented"), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("AUTH_REFRESH_REUSED", result.Error!.Code);
        _unitOfWork.Verify(x => x.ClearTrackedChanges(), Times.Once);
        _refreshTokens.Verify(x => x.RevokeAllForFamilyAsync(
            fixture.Token.FamilyId,
            "refresh-token-reuse",
            fixture.Now,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void ConfigureActiveFixture(Fixture fixture)
    {
        _clock.SetupGet(x => x.UtcNow).Returns(fixture.Now);
        _tokens.Setup(x => x.HashRefreshToken("presented")).Returns("presented-hash");
        _refreshTokens.Setup(x => x.GetByHashAsync("presented-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fixture.Token);
        _users.Setup(x => x.GetByIdAsync(fixture.User.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fixture.User);
        _sessions.Setup(x => x.GetActiveAsync(fixture.Session.Id, fixture.User.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fixture.Session);
    }

    private RefreshTokenCommandHandler CreateHandler() => new(
        _users.Object,
        _sessions.Object,
        _sessionValidation.Object,
        _refreshTokens.Object,
        _tokens.Object,
        _audit.Object,
        _unitOfWork.Object,
        _clock.Object);

    private static Fixture CreateFixture()
    {
        var now = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var user = User.Create("refresh@example.com", "Refresh User", "password-hash", now);
        var session = UserSession.Create(user.Id, "browser", "ip-hash", "ua-hash", now.AddDays(30), now);
        var token = GodForge.Domain.Entities.Identity.RefreshToken.Create(
            user.Id,
            session.Id,
            Guid.NewGuid(),
            "presented-hash",
            now.AddDays(30),
            now);
        return new Fixture(now, user, session, token);
    }

    private sealed record Fixture(
        DateTimeOffset Now,
        User User,
        UserSession Session,
        GodForge.Domain.Entities.Identity.RefreshToken Token);
}