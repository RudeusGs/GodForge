using System.Security.Cryptography;
using System.Text;
using GodForge.Application.Common.Interfaces;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Security;

public sealed class SecretHashService : ISecretHashService
{
    private readonly byte[] _key;

    public SecretHashService(IOptions<JwtSettings> options)
    {
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.Secret));
    }

    public string Hash(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        using var hmac = new HMACSHA256(_key);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(secret)));
    }

    public bool Verify(string secret, string expectedHash)
    {
        try
        {
            var actual = Convert.FromBase64String(Hash(secret));
            var expected = Convert.FromBase64String(expectedHash);
            return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
