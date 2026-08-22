using GodForge.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace GodForge.UnitTests.Api.Services;

public sealed class RedisDistributedAuthRateLimiterTests
{
    [Fact]
    public async Task ConsumeAsync_WhenRedisFails_ReturnsDependencyUnavailableInsteadOfFailingOpen()
    {
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(value => value.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "unavailable"));
        var limiter = new RedisDistributedAuthRateLimiter(
            redis.Object,
            Mock.Of<ILogger<RedisDistributedAuthRateLimiter>>());

        var decision = await limiter.ConsumeAsync("login", "scope:test", 5, TimeSpan.FromMinutes(1), CancellationToken.None);

        Assert.False(decision.Allowed);
        Assert.False(decision.DependencyAvailable);
    }
}
