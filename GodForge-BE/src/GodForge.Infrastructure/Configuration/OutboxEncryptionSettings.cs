using System.ComponentModel.DataAnnotations;

namespace GodForge.Infrastructure.Configuration;

public sealed class OutboxEncryptionSettings
{
    public const string SectionName = "OutboxEncryption";

    [Required(ErrorMessage = "Outbox encryption key is required.")]
    [MinLength(32, ErrorMessage = "Outbox encryption key must contain at least 32 characters.")]
    public string Key { get; set; } = string.Empty;

    public string? LegacyKey { get; set; }
}
