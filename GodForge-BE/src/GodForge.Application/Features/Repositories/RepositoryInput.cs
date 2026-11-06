using System.Text.RegularExpressions;

namespace GodForge.Application.Features.Repositories;

public static partial class RepositoryInput
{
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex NamePattern();
    [GeneratedRegex("^[a-fA-F0-9]{40}$", RegexOptions.CultureInvariant)]
    private static partial Regex ShaPattern();

    public static bool ValidName(string? value) => value is { Length: > 0 and <= 60 } && NamePattern().IsMatch(value);
    public static bool ValidSha(string? value) => value is not null && ShaPattern().IsMatch(value);
    public static bool ValidBranch(string? value) => value is { Length: > 0 and <= 150 } && value != "@" &&
        !value.StartsWith('-') && !value.EndsWith('.') && !value.Contains("..", StringComparison.Ordinal) &&
        !value.Contains("@{", StringComparison.Ordinal) && !value.Any(c => char.IsControl(c) || char.IsWhiteSpace(c) || "~^:?*[\\%".Contains(c)) &&
        value.Split('/').All(p => p.Length > 0 && !p.StartsWith('.') && !p.EndsWith(".lock", StringComparison.OrdinalIgnoreCase));

    public static bool ValidPath(string? value, bool allowEmpty = false) => value is not null && value.Length <= 1000 &&
        (allowEmpty && value.Length == 0 || value.Length > 0 &&
        !value.Any(c => char.IsControl(c) || "\\%:#?".Contains(c)) &&
        value.Split('/').Length <= 32 && value.Split('/').All(p => p.Length > 0 && p is not ("." or "..")));
}
