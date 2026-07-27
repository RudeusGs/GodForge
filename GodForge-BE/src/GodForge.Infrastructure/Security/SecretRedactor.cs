using System.Text.RegularExpressions;
using GodForge.Application.Common.Interfaces;

namespace GodForge.Infrastructure.Security;

public sealed class SecretRedactor : ISecretRedactor
{
    private const string Marker = "[REDACTED_SECRET]";
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly RedactionRule[] Rules =
    {
        new(
            new Regex("(?im)^(\\s*[\"']?(?:api[_-]?key|secret|token|password|passwd|private[_-]?key|client[_-]?secret|access[_-]?key)[\"']?\\s*[:=]\\s*)[^\\r\\n]+$", RegexOptions.Compiled, RegexTimeout),
            $"$1{Marker}"),
        new(
            new Regex("(?i)(\\\"(?:apiKey|api_key|secret|token|password|passwd|privateKey|private_key|clientSecret|client_secret|accessKey|access_key)\\\"\\s*:\\s*)\\\"[^\\\"]*\\\"", RegexOptions.Compiled, RegexTimeout),
            $"$1\"{Marker}\""),
        new(
            new Regex(@"(?i)(\b(?:Password|Pwd)\s*=\s*)[^;\r\n]+", RegexOptions.Compiled, RegexTimeout),
            $"$1{Marker}"),
        new(
            new Regex(@"(?i)bearer\s+[a-z0-9._~+/=-]{16,}", RegexOptions.Compiled, RegexTimeout),
            $"Bearer {Marker}"),
        new(
            new Regex(@"(?i)(?:ghp|github_pat|glpat)-?[a-z0-9_\-]{16,}", RegexOptions.Compiled, RegexTimeout),
            Marker),
        new(
            new Regex(@"\bAKIA[0-9A-Z]{16}\b", RegexOptions.Compiled, RegexTimeout),
            Marker),
        new(
            new Regex(@"\bAIza[0-9A-Za-z_\-]{30,}\b", RegexOptions.Compiled, RegexTimeout),
            Marker),
        new(
            new Regex(@"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----[\s\S]*?-----END (?:RSA |EC |OPENSSH )?PRIVATE KEY-----", RegexOptions.Compiled, RegexTimeout),
            Marker),
        new(
            new Regex(@"(?i)((?:postgres(?:ql)?|mysql|mongodb(?:\+srv)?|redis)://[^\s:@/]+:)[^\s@/]+@", RegexOptions.Compiled, RegexTimeout),
            $"$1{Marker}@")
    };

    public string Redact(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var output = content;
        foreach (var rule in Rules)
        {
            output = rule.Pattern.Replace(output, rule.Replacement);
        }

        return output;
    }

    private sealed record RedactionRule(Regex Pattern, string Replacement);
}
