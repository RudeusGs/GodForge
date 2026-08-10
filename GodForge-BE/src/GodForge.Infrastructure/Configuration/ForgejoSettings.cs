using System.ComponentModel.DataAnnotations;

namespace GodForge.Infrastructure.Configuration;

public sealed class ForgejoSettings
{
    public bool Enabled { get; set; }

    [MaxLength(500)]
    public string BaseUrl { get; set; } = "http://localhost:3000";

    [MaxLength(1000)]
    public string ApiToken { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string WebhookSecret { get; set; } = string.Empty;
}
