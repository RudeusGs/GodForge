using GodForge.Application.Common.Interfaces;

namespace GodForge.Infrastructure.Security;

public sealed class TokenBlacklistService : ITokenBlacklistService
{
    private readonly ICacheService _cache;
    private const string KeyPrefix = "blacklist:jti:";

    public TokenBlacklistService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task BlacklistTokenAsync(string jti, TimeSpan expiresIn, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{jti}";
        await _cache.SetAsync(key, "revoked", absoluteExpireTime: expiresIn, cancellationToken: cancellationToken);
    }

    public async Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{jti}";
        var value = await _cache.GetAsync<string>(key, cancellationToken);
        return value != null;
    }
}
