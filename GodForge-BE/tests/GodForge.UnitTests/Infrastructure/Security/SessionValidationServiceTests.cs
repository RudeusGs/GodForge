using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Infrastructure.Configuration;
using GodForge.Infrastructure.Security;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace GodForge.UnitTests.Infrastructure.Security;

public sealed class SessionValidationServiceTests
{
    [Fact]
    public async Task IsValidAsync_ReusesPositiveCacheEntry()
    {
        var now = DateTimeOffset.UtcNow;
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessions = new Mock<IUserSessionRepository>();
        sessions.Setup(x => x.GetValidUntilAsync(sessionId, userId, "stamp", now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(now.AddMinutes(5));
        var service = CreateService(sessions.Object, new TestDistributedCache());

        Assert.True(await service.IsValidAsync(sessionId, userId, "stamp", now));
        Assert.True(await service.IsValidAsync(sessionId, userId, "stamp", now));

        sessions.Verify(x => x.GetValidUntilAsync(sessionId, userId, "stamp", now, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateSessionAsync_BypassesPositiveCacheAndForcesDatabaseValidation()
    {
        var now = DateTimeOffset.UtcNow;
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessions = new Mock<IUserSessionRepository>();
        sessions.Setup(x => x.GetValidUntilAsync(sessionId, userId, "stamp", now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(now.AddMinutes(5));
        var service = CreateService(sessions.Object, new TestDistributedCache());

        Assert.True(await service.IsValidAsync(sessionId, userId, "stamp", now));
        await service.InvalidateSessionAsync(sessionId);
        Assert.True(await service.IsValidAsync(sessionId, userId, "stamp", now));
        Assert.True(await service.IsValidAsync(sessionId, userId, "stamp", now));

        sessions.Verify(x => x.GetValidUntilAsync(sessionId, userId, "stamp", now, It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task IsValidAsync_DoesNotTrustCachedEntryForDifferentSecurityStamp()
    {
        var now = DateTimeOffset.UtcNow;
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessions = new Mock<IUserSessionRepository>();
        sessions.Setup(x => x.GetValidUntilAsync(sessionId, userId, "stamp-a", now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(now.AddMinutes(5));
        sessions.Setup(x => x.GetValidUntilAsync(sessionId, userId, "stamp-b", now, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTimeOffset?)null);
        var service = CreateService(sessions.Object, new TestDistributedCache());

        Assert.True(await service.IsValidAsync(sessionId, userId, "stamp-a", now));
        Assert.False(await service.IsValidAsync(sessionId, userId, "stamp-b", now));

        sessions.Verify(x => x.GetValidUntilAsync(sessionId, userId, "stamp-b", now, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsValidAsync_WhenInvalidationAppearsDuringDatabaseValidation_DoesNotPublishReusablePositiveEntry()
    {
        var now = DateTimeOffset.UtcNow;
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessions = new Mock<IUserSessionRepository>();
        var cache = new TestDistributedCache();
        SessionValidationService? service = null;

        sessions.Setup(x => x.GetValidUntilAsync(sessionId, userId, "stamp", now, It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await service!.InvalidateSessionAsync(sessionId);
                return now.AddMinutes(5);
            });
        service = CreateService(sessions.Object, cache);

        Assert.True(await service.IsValidAsync(sessionId, userId, "stamp", now));
        Assert.True(await service.IsValidAsync(sessionId, userId, "stamp", now));

        sessions.Verify(x => x.GetValidUntilAsync(sessionId, userId, "stamp", now, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task InvalidateSessionAsync_WhenBarrierCannotBePublished_FailsBeforeRevocationCanCommit()
    {
        var sessions = Mock.Of<IUserSessionRepository>();
        var cache = new TestDistributedCache(failSet: key => key.Contains("session-validation-bypass", StringComparison.Ordinal));
        var service = CreateService(sessions, cache);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.InvalidateSessionAsync(Guid.NewGuid()));

        Assert.Contains("safely published", exception.Message);
    }

    private static SessionValidationService CreateService(IUserSessionRepository sessions, IDistributedCache cache)
        => new(
            sessions,
            cache,
            Options.Create(new JwtSettings
            {
                ExpiryMinutes = 15,
                SessionValidationCacheSeconds = 10
            }),
            Mock.Of<ILogger<SessionValidationService>>());

    private sealed class TestDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _values = new(StringComparer.Ordinal);
        private readonly Func<string, bool>? _failSet;

        public TestDistributedCache(Func<string, bool>? failSet = null)
        {
            _failSet = failSet;
        }

        public byte[]? Get(string key) => _values.TryGetValue(key, out var value) ? value : null;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
            => Task.FromResult(Get(key));

        public void Refresh(string key) { }

        public Task RefreshAsync(string key, CancellationToken token = default)
            => Task.CompletedTask;

        public void Remove(string key) => _values.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (_failSet?.Invoke(key) == true)
                throw new InvalidOperationException("simulated cache write failure");

            _values[key] = value;
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }
}
