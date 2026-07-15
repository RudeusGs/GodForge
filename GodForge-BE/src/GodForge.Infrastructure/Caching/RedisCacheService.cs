using System.Text.Json;
using GodForge.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GodForge.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IDistributedCache distributedCache,
        IMemoryCache memoryCache,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedData = await _distributedCache.GetStringAsync(key, cancellationToken);
            return string.IsNullOrEmpty(cachedData)
                ? default
                : JsonSerializer.Deserialize<T>(cachedData);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Distributed cache failed to GET key {Key}; using the local mirror.",
                key);
            return _memoryCache.TryGetValue(key, out T? value) ? value : default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpireTime = null,
        TimeSpan? unusedExpireTime = null,
        CancellationToken cancellationToken = default)
    {
        var memoryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpireTime,
            SlidingExpiration = unusedExpireTime
        };

        // Keep a same-process mirror even while Redis is healthy. If Redis becomes unavailable,
        // OTPs and revoked token markers written by this instance remain available locally.
        _memoryCache.Set(key, value, memoryOptions);

        try
        {
            var distributedOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpireTime,
                SlidingExpiration = unusedExpireTime
            };
            var serializedData = JsonSerializer.Serialize(value);
            await _distributedCache.SetStringAsync(
                key,
                serializedData,
                distributedOptions,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Distributed cache failed to SET key {Key}; the local mirror remains active.",
                key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Distributed cache failed to REMOVE key {Key}; the local mirror was cleared.",
                key);
        }
    }
}
