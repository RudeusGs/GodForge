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
    public async Task IsValidAsync_ValidatesUserStampStatusAndSessionInSingleRepositoryQuery()
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
            Assert.True(await repository.IsValidAsync(sessionId, userId, securityStamp, _now));
            Assert.False(await repository.IsValidAsync(sessionId, userId, "wrong-stamp", _now));
            Assert.False(await repository.IsValidAsync(sessionId, userId, securityStamp, _now.AddHours(2)));
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
        Assert.False(await deletedUserRepository.IsValidAsync(sessionId, userId, securityStamp, _now.AddMinutes(2)));
    }
}
