using System.ComponentModel.DataAnnotations;

namespace GodForge.Infrastructure.Configuration;

public sealed class SecretHashSettings
{
    public const string SectionName = "SecretHashing";

    [Required]
    [MinLength(32)]
    public string Key { get; set; } = string.Empty;

    public string LegacyKey { get; set; } = string.Empty;
}
