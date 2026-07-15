using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GodForge.Application.Common.Text;

public static partial class SlugGenerator
{
    public static string Generate(string value, int maxLength = 80)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (maxLength < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength));
        }

        var normalized = value.Trim()
            .Replace('đ', 'd')
            .Replace('Đ', 'D')
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var slug = NonAlphaNumericRegex()
            .Replace(builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant(), "-")
            .Trim('-');

        if (slug.Length > maxLength)
        {
            slug = slug[..maxLength].TrimEnd('-');
        }

        return string.IsNullOrWhiteSpace(slug)
            ? $"project-{Guid.NewGuid():N}"[..16]
            : slug;
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonAlphaNumericRegex();
}
