using System.Text.RegularExpressions;
using GodForge.Application.Common.Interfaces;

namespace GodForge.Infrastructure.Security;

public sealed class SecretRedactor : ISecretRedactor
{
    private const string Replacement = "[REDACTED_SECRET]";
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly Regex[] Patterns =
    {
        new(@"(?im)^(\s*(?:api[_-]?key|secret|token|password|passwd|private[_-]?key|client[_-]?secret)\s*[:=]\s*)[^\r\n]+$", RegexOptions.Compiled, RegexTimeout),
        new(@"(?i)bearer\s+[a-z0-9._~+/=-]{16,}", RegexOptions.Compiled, RegexTimeout),
        new(@"(?i)(?:ghp|github_pat|glpat)-?[a-z0-9_\-]{16,}", RegexOptions.Compiled, RegexTimeout),
        new(@"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----[\s\S]*?-----END (?:RSA |EC |OPENSSH )?PRIVATE KEY-----", RegexOptions.Compiled, RegexTimeout),
        new(@"(?i)(postgres(?:ql)?|mysql|mongodb(?:\+srv)?|redis)://[^\s:@/]+:[^\s@/]+@", RegexOptions.Compiled, RegexTimeout)
    };

    public string Redact(string content)
    {
        var output = content;
        foreach (var pattern in Patterns)
        {
            output = pattern.Replace(output, match =>
            {
                if (match.Groups.Count > 1 && match.Groups[1].Success)
                {
                    return match.Groups[1].Value + Replacement;
                }

                return Replacement;
            });
        }

        return output;
    }
}
