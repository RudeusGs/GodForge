using GodForge.Domain.Entities.Identity;
using GodForge.Infrastructure.Persistence.Repositories;
using GodForge.IntegrationTests.Persistence;

namespace GodForge.IntegrationTests.Identity;

[Collection(PostgresPersistenceCollection.CollectionName)]
public sealed class UserSessionRepositoryTests
{
    private readonly PostgresPersistenceFixture _fixture;
    private readonly DateTimeOffset _now = new(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);

    public UserSessionRepositoryTests(PostgresPersistenceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetValidUntilAsync_ValidatesUserStampStatusAndSessionInSingleRepositoryQuery()
    {
        Guid userId;
        Guid sessionId;
        string securityStamp;
        await using (var seedContext = _fixture.CreateContext())
        {
            var user = User.Create(
                $"session-{Guid.NewGuid():N}@example.com",
                "Session User",
                "password-hash",
                _now);
            var session = UserSession.Create(
                user.Id,
                "test-device",
                "127.0.0.1",
                "test-agent",
                _now.AddHours(1),
                _now);
            seedContext.AddRange(user, session);
            await seedContext.SaveChangesAsync();
            userId = user.Id;
            sessionId = session.Id;
            securityStamp = user.SecurityStamp;
        }

        await using (var validationContext = _fixture.CreateContext())
        {
            var repository = new UserSessionRepository(validationContext);
            Assert.Equal(_now.AddHours(1), await repository.GetValidUntilAsync(sessionId, userId, securityStamp, _now));
            Assert.Null(await repository.GetValidUntilAsync(sessionId, userId, "wrong-stamp", _now));
            Assert.Null(await repository.GetValidUntilAsync(sessionId, userId, securityStamp, _now.AddHours(2)));
        }

        await using (var deleteContext = _fixture.CreateContext())
        {
            var user = await deleteContext.Users.FindAsync(userId);
            Assert.NotNull(user);
            user.SoftDelete(_now.AddMinutes(1));
            await deleteContext.SaveChangesAsync();
        }

        await using var deletedUserContext = _fixture.CreateContext();
        var deletedUserRepository = new UserSessionRepository(deletedUserContext);
        Assert.Null(await deletedUserRepository.GetValidUntilAsync(sessionId, userId, securityStamp, _now.AddMinutes(2)));
    }

    [Fact]
    public async Task GetForUserAsync_PrioritizesActiveSessionsBeforeRecentRevokedHistoryWithinLimit()
    {
        Guid userId;
        Guid currentSessionId;
        Guid olderActiveSessionId;
        Guid recentRevokedSessionId;

        await using (var seedContext = _fixture.CreateContext())
        {
            var user = User.Create(
                $"session-list-{Guid.NewGuid():N}@example.com",
                "Session List User",
                "password-hash",
                _now.AddDays(-30));
            var currentSession = UserSession.Create(
                user.Id,
                "current",
                null,
                null,
                _now.AddDays(30),
                _now.AddDays(-10));
            currentSession.RecordActivity(_now);
            var olderActiveSession = UserSession.Create(
                user.Id,
                "older-active",
                null,
                null,
                _now.AddDays(30),
                _now.AddDays(-20));
            var recentRevokedSession = UserSession.Create(
                user.Id,
                "recent-revoked",
                null,
                null,
                _now.AddDays(30),
                _now.AddDays(-1));
            recentRevokedSession.Revoke("test", _now.AddHours(-1));

            seedContext.AddRange(user, currentSession, olderActiveSession, recentRevokedSession);
            await seedContext.SaveChangesAsync();

            userId = user.Id;
            currentSessionId = currentSession.Id;
            olderActiveSessionId = olderActiveSession.Id;
            recentRevokedSessionId = recentRevokedSession.Id;
        }

        await using var queryContext = _fixture.CreateContext();
        var repository = new UserSessionRepository(queryContext);
        var sessions = await repository.GetForUserAsync(userId, currentSessionId, _now, 2);

        Assert.Equal(2, sessions.Count);
        Assert.Equal(currentSessionId, sessions[0].Id);
        Assert.Contains(sessions, session => session.Id == olderActiveSessionId);
        Assert.DoesNotContain(sessions, session => session.Id == recentRevokedSessionId);
    }
}
