using StackExchange.Redis;

namespace GodForge.Api.Services;

public interface IDistributedAuthRateLimiter
{
    Task<DistributedRateLimitDecision> ConsumeAsync(
        string policy,
        string partition,
        int permitLimit,
        TimeSpan window,
        CancellationToken cancellationToken);
}

public sealed record DistributedRateLimitDecision(bool Allowed, bool DependencyAvailable, TimeSpan RetryAfter)
{
    public static DistributedRateLimitDecision Permit() => new(true, true, TimeSpan.Zero);
    public static DistributedRateLimitDecision Reject(TimeSpan retryAfter) => new(false, true, retryAfter);
    public static DistributedRateLimitDecision Unavailable() => new(false, false, TimeSpan.Zero);
}

public sealed class RedisDistributedAuthRateLimiter : IDistributedAuthRateLimiter
{
    private const string Script = """
        local current = redis.call('INCR', KEYS[1])
        if current == 1 then
            redis.call('PEXPIRE', KEYS[1], ARGV[2])
        end
        local ttl = redis.call('PTTL', KEYS[1])
        return { current, ttl }
        """;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedAuthRateLimiter> _logger;

    public RedisDistributedAuthRateLimiter(
        IConnectionMultiplexer redis,
        ILogger<RedisDistributedAuthRateLimiter> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<DistributedRateLimitDecision> ConsumeAsync(
        string policy,
        string partition,
        int permitLimit,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var result = (RedisResult[]?)await _redis.GetDatabase().ScriptEvaluateAsync(
                Script,
                [$"auth:rate:{policy}:{partition}"],
                [permitLimit, Math.Max(1, (long)window.TotalMilliseconds)]);
            if (result is null || result.Length != 2)
                throw new InvalidOperationException("Redis returned an invalid auth rate-limit result.");

            var count = (long)result[0];
            var ttlMilliseconds = Math.Max(1, (long)result[1]);
            return count <= permitLimit
                ? DistributedRateLimitDecision.Permit()
                : DistributedRateLimitDecision.Reject(TimeSpan.FromMilliseconds(ttlMilliseconds));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Distributed auth rate limiter failed for policy {Policy}.", policy);
            return DistributedRateLimitDecision.Unavailable();
        }
    }
}

public sealed class DevelopmentAuthRateLimiter : IDistributedAuthRateLimiter
{
    public Task<DistributedRateLimitDecision> ConsumeAsync(
        string policy,
        string partition,
        int permitLimit,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        _ = policy;
        _ = partition;
        _ = permitLimit;
        _ = window;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(DistributedRateLimitDecision.Permit());
    }
}
