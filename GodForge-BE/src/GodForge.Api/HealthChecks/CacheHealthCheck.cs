using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GodForge.Api.HealthChecks;

public sealed class CacheHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;

    public CacheHealthCheck(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var key = $"health:{Guid.NewGuid():N}";
        try
        {
            await _cache.SetStringAsync(
                key,
                "ok",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                },
                cancellationToken);
            var value = await _cache.GetStringAsync(key, cancellationToken);
            await _cache.RemoveAsync(key, cancellationToken);

            return value == "ok"
                ? HealthCheckResult.Healthy("Distributed cache is reachable.")
                : HealthCheckResult.Unhealthy("Distributed cache read/write verification failed.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Distributed cache health check failed.", exception);
        }
    }
}
