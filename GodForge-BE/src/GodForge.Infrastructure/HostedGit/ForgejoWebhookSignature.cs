using System.Security.Cryptography;
using System.Text;
using GodForge.Application.Common.Interfaces;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.HostedGit;

public sealed class ForgejoWebhookSignature(IOptions<ForgejoSettings> settings) : IForgejoWebhookSignature
{
    public bool Verify(byte[] body, string signature)
    {
        if (!settings.Value.Enabled || settings.Value.WebhookSecret.Length < 32 || signature.Length != 64) return false;
        byte[] supplied;
        try { supplied = Convert.FromHexString(signature); }
        catch (FormatException) { return false; }
        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(settings.Value.WebhookSecret), body);
        return CryptographicOperations.FixedTimeEquals(expected, supplied);
    }
}
