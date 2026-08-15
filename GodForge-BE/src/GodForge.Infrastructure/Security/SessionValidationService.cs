using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Security;

public sealed class SessionValidationService : ISessionValidationService
{
    private const string CacheKeyPrefix = "auth:session-validation:";
    private const string CacheBypassKeyPrefix = "auth:session-validation-bypass:";
    private readonly IUserSessionRepository _sessions;
    private readonly IDistributedCache _cache;
    private readonly JwtSettings _settings;
    private readonly ILogger<SessionValidationService> _logger;
    private readonly bool _enablePositiveCache;

    public SessionValidationService(
        IUserSessionRepository sessions,
        IDistributedCache cache,
        IOptions<JwtSettings> settings,
        ILogger<SessionValidationService> logger,
        bool enablePositiveCache = true)
    {
        _sessions = sessions;
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
        _enablePositiveCache = enablePositiveCache;
    }

    public async Task<bool> IsValidAsync(
        Guid sessionId,
        Guid userId,
        string securityStamp,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (!_enablePositiveCache)
            return await IsValidInDatabaseAsync(sessionId, userId, securityStamp, now, cancellationToken);

        var cacheKey = GetCacheKey(sessionId);
        var bypassKey = GetCacheBypassKey(sessionId);
        var expectedStampHash = HashSecurityStamp(securityStamp);
        var cacheAvailable = true;

        try
        {
            if (await HasCacheBypassAsync(bypassKey, cancellationToken))
                return await IsValidInDatabaseAsync(sessionId, userId, securityStamp, now, cancellationToken);

            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                var entry = JsonSerializer.Deserialize<SessionValidationCacheEntry>(cached);
                if (entry is not null &&
                    entry.UserId == userId &&
                    FixedTimeEquals(entry.SecurityStampHash, expectedStampHash))
                {
                    // Re-check the bypass marker so a revocation racing with the cache read cannot
                    // leave a trusted positive entry after the revocation barrier is published.
                    if (!await HasCacheBypassAsync(bypassKey, cancellationToken))
                        return true;

                    return await IsValidInDatabaseAsync(sessionId, userId, securityStamp, now, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            cacheAvailable = false;
            _logger.LogWarning(exception, "Session validation cache read failed for session {SessionId}; falling back to the database.", sessionId);
        }

        var validUntil = await _sessions.GetValidUntilAsync(sessionId, userId, securityStamp, now, cancellationToken);
        if (validUntil is null)
            return false;

        var remaining = validUntil.Value - now;
        if (remaining <= TimeSpan.Zero)
            return false;

        // Cache is an optimization only. When it is unavailable, the database result is authoritative
        // for this request and no positive entry is published.
        if (!cacheAvailable)
            return true;

        var cacheDuration = TimeSpan.FromSeconds(_settings.SessionValidationCacheSeconds);
        if (remaining < cacheDuration)
            cacheDuration = remaining;

        var positiveEntryWritten = false;
        try
        {
            // Revocation handlers publish this barrier before committing the database mutation.
            // If it appears while validation is in flight, do not publish a new positive cache entry.
            if (await HasCacheBypassAsync(bypassKey, cancellationToken))
                return true;

            var payload = JsonSerializer.Serialize(new SessionValidationCacheEntry(userId, expectedStampHash));
            await _cache.SetStringAsync(
                cacheKey,
                payload,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = cacheDuration },
                cancellationToken);
            positiveEntryWritten = true;

            // Close the write-after-invalidate race. The bypass marker intentionally outlives any
            // positive validation entry, so even a failed best-effort removal cannot revive a session.
            if (await HasCacheBypassAsync(bypassKey, cancellationToken))
                await TryRemovePositiveEntryAsync(cacheKey, sessionId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (positiveEntryWritten)
                await TryRemovePositiveEntryAsync(cacheKey, sessionId, CancellationToken.None);

            _logger.LogWarning(exception, "Session validation cache write failed for session {SessionId}.", sessionId);
        }

        return true;
    }

    public async Task InvalidateSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (!_enablePositiveCache)
            return;

        var bypassKey = GetCacheBypassKey(sessionId);
        var bypassDuration = TimeSpan.FromMinutes(_settings.ExpiryMinutes)
            + TimeSpan.FromSeconds(_settings.SessionValidationCacheSeconds)
            + TimeSpan.FromMinutes(1);

        try
        {
            // This marker is the security barrier. Revocation flows call this before SaveChanges so a
            // cache outage cannot commit a revoked session while an old positive cache entry is trusted.
            await _cache.SetStringAsync(
                bypassKey,
                "1",
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = bypassDuration },
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Session revocation cache barrier could not be published for session {SessionId}.", sessionId);
            throw new InvalidOperationException("Session revocation could not be safely published to the validation cache.", exception);
        }

        // Removing the positive entry is an optimization. The bypass marker above is authoritative for
        // cache use and outlives every positive entry, so removal failure cannot re-authorize a session.
        await TryRemovePositiveEntryAsync(GetCacheKey(sessionId), sessionId, cancellationToken);
    }

    public async Task InvalidateSessionsAsync(
        IReadOnlyCollection<Guid> sessionIds,
        CancellationToken cancellationToken = default)
    {
        foreach (var sessionId in sessionIds.Distinct())
            await InvalidateSessionAsync(sessionId, cancellationToken);
    }

    private async Task<bool> IsValidInDatabaseAsync(
        Guid sessionId,
        Guid userId,
        string securityStamp,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => await _sessions.GetValidUntilAsync(sessionId, userId, securityStamp, now, cancellationToken) is { } validUntil
           && validUntil > now;

    private async Task<bool> HasCacheBypassAsync(string bypassKey, CancellationToken cancellationToken)
        => !string.IsNullOrWhiteSpace(await _cache.GetStringAsync(bypassKey, cancellationToken));

    private async Task TryRemovePositiveEntryAsync(
        string cacheKey,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Session validation positive cache entry could not be removed for session {SessionId}; the bypass marker remains authoritative.",
                sessionId);
        }
    }

    private static string GetCacheKey(Guid sessionId) => $"{CacheKeyPrefix}{sessionId:N}";

    private static string GetCacheBypassKey(Guid sessionId) => $"{CacheBypassKeyPrefix}{sessionId:N}";

    private static string HashSecurityStamp(string securityStamp)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp)));

    private static bool FixedTimeEquals(string left, string right)
    {
        if (left.Length != right.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(left),
            Encoding.ASCII.GetBytes(right));
    }

    private sealed record SessionValidationCacheEntry(Guid UserId, string SecurityStampHash);
}
