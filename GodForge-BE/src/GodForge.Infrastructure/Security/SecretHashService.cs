using System.Security.Cryptography;
using System.Text;
using GodForge.Application.Common.Interfaces;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Security;

public sealed class SecretHashService : ISecretHashService
{
    private readonly byte[] _key;
    private readonly byte[]? _legacyKey;

    public SecretHashService(IOptions<SecretHashSettings> options)
    {
        _key = DeriveKey(options.Value.Key);
        _legacyKey = string.IsNullOrWhiteSpace(options.Value.LegacyKey)
            ? null
            : DeriveKey(options.Value.LegacyKey);
    }

    public string Hash(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        return ComputeHash(secret, _key);
    }

    public bool Verify(string secret, string expectedHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        if (!TryDecodeHash(expectedHash, out var expected))
            return false;

        if (Matches(secret, expected, _key))
            return true;

        return _legacyKey is not null && Matches(secret, expected, _legacyKey);
    }

    private static bool Matches(string secret, byte[] expected, byte[] key)
    {
        var actual = Convert.FromBase64String(ComputeHash(secret, key));
        return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static string ComputeHash(string secret, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(secret)));
    }

    private static byte[] DeriveKey(string key)
        => SHA256.HashData(Encoding.UTF8.GetBytes(key));

    private static bool TryDecodeHash(string expectedHash, out byte[] expected)
    {
        try
        {
            expected = Convert.FromBase64String(expectedHash);
            return true;
        }
        catch (FormatException)
        {
            expected = Array.Empty<byte>();
            return false;
        }
    }
}
