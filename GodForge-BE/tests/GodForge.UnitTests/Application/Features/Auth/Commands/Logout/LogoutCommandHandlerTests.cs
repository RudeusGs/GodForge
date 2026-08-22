using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Features.Auth.Commands.Logout;
using GodForge.Domain.Entities.Identity;
using Moq;
using Xunit;

namespace GodForge.UnitTests.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandlerTests
{
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<ITokenBlacklistService> _blacklist = new();
    private readonly Mock<ISessionValidationService> _sessionValidation = new();
    private readonly Mock<IUserSessionRepository> _sessions = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IAuditWriter> _auditWriter = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _handler = new LogoutCommandHandler(
            _currentUser.Object,
            _blacklist.Object,
            _sessionValidation.Object,
            _sessions.Object,
            _refreshTokens.Object,
            _auditWriter.Object,
            _unitOfWork.Object,
            _clock.Object);
    }

    [Fact]
    public async Task Handle_RevokesCurrentSessionRefreshTokensAndBlacklistsAccessToken()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var jti = Guid.NewGuid().ToString("N");
        var session = UserSession.Create(userId, "browser", null, null, now.AddDays(30), now);
        var sessionId = session.Id;
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _currentUser.Setup(x => x.GetId()).Returns(userId);
        _currentUser.Setup(x => x.GetSessionId()).Returns(sessionId);
        _currentUser.SetupGet(x => x.Jti).Returns(jti);
        _currentUser.SetupGet(x => x.TokenExpiration).Returns(now.AddMinutes(10));
        _sessions.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        var sequence = new MockSequence();
        _sessionValidation.InSequence(sequence)
            .Setup(x => x.InvalidateSessionAsync(sessionId, CancellationToken.None))
            .Returns(Task.CompletedTask);
        _unitOfWork.InSequence(sequence)
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ReturnsAsync(1);

        var result = await _handler.Handle(new LogoutCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(session.RevokedAt);
        _refreshTokens.Verify(x => x.RevokeAllForSessionAsync(sessionId, "logout", now, It.IsAny<CancellationToken>()), Times.Once);
        _blacklist.Verify(x => x.BlacklistTokenAsync(jti, It.Is<TimeSpan>(t => t > TimeSpan.FromMinutes(9)), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sessionValidation.Verify(x => x.InvalidateSessionAsync(sessionId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSessionDoesNotBelongToUser_DoesNotRevokeEntityButStillRevokesTokenScope()
    {
        var now = DateTimeOffset.UtcNow;
        var currentUserId = Guid.NewGuid();
        var foreignSession = UserSession.Create(Guid.NewGuid(), null, null, null, now.AddDays(1), now);

        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _currentUser.Setup(x => x.GetId()).Returns(currentUserId);
        _currentUser.Setup(x => x.GetSessionId()).Returns(foreignSession.Id);
        _sessions.Setup(x => x.GetByIdAsync(foreignSession.Id, It.IsAny<CancellationToken>())).ReturnsAsync(foreignSession);

        var result = await _handler.Handle(new LogoutCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(foreignSession.RevokedAt);
        _refreshTokens.Verify(x => x.RevokeAllForSessionAsync(foreignSession.Id, "logout", now, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRedisInvalidationBarrierFails_DoesNotCommitRevocation()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var session = UserSession.Create(userId, "browser", null, null, now.AddDays(30), now);
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _currentUser.Setup(x => x.GetId()).Returns(userId);
        _currentUser.Setup(x => x.GetSessionId()).Returns(session.Id);
        _sessions.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _sessionValidation.Setup(x => x.InvalidateSessionAsync(session.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("redis unavailable"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(new LogoutCommand(), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
