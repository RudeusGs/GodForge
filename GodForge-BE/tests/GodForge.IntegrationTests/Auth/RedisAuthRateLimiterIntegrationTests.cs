using GodForge.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace GodForge.IntegrationTests.Auth;

public sealed class RedisAuthRateLimiterIntegrationTests
{
    [Fact]
    public async Task ConsumeAsync_ConcurrentDistributedIncrements_AllowOnlyConfiguredLimit()
    {
        var connectionString = Environment.GetEnvironmentVariable("GODFORGE_TEST_REDIS") ?? "localhost:6379";
        ConnectionMultiplexer redis;
        try
        {
            redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
        }
        catch (RedisConnectionException)
        {
            return; // Skip test gracefully if Redis is not available in the environment
        }

        await using (redis)
        {
            var limiter = new RedisDistributedAuthRateLimiter(
                redis,
                Mock.Of<ILogger<RedisDistributedAuthRateLimiter>>());
            var partition = $"scope:integration-{Guid.NewGuid():N}";

            try
            {
                var decisions = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ =>
                    limiter.ConsumeAsync("integration", partition, 5, TimeSpan.FromMinutes(1), CancellationToken.None)));

                Assert.Equal(5, decisions.Count(decision => decision.Allowed));
                Assert.Equal(15, decisions.Count(decision => !decision.Allowed && decision.DependencyAvailable));
                Assert.All(decisions.Where(decision => !decision.Allowed), decision => Assert.True(decision.RetryAfter > TimeSpan.Zero));
            }
            finally
            {
                await redis.GetDatabase().KeyDeleteAsync($"auth:rate:integration:{partition}");
            }
        }
    }
}
