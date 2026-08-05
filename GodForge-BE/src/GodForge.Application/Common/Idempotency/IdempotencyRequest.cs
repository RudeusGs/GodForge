using System.Security.Cryptography;
using System.Text;
using GodForge.Application.Common.Models;

namespace GodForge.Application.Common.Idempotency;

public static class IdempotencyRequest
{
    public static ApplicationError? Normalize(string? value, out string? normalized)
    {
        normalized = null;
        if (value is null)
            return null;
        normalized = value.Trim();
        if (normalized.Length is < 1 or > 160)
            return ApplicationError.Validation("VALIDATION_ERROR", "Idempotency-Key must contain between 1 and 160 characters.");
        if (normalized.Any(char.IsControl))
            return ApplicationError.Validation("VALIDATION_ERROR", "Idempotency-Key contains invalid control characters.");
        return null;
    }

    public static string Hash(string canonicalRequest)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));
}
