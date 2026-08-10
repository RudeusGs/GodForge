using System.ComponentModel.DataAnnotations;

namespace GodForge.Infrastructure.Configuration;

public sealed class GeminiSettings
{
    public bool Enabled { get; set; }

    public string ApiKey { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Model { get; set; } = "gemini-2.5-flash";

    [MaxLength(500)]
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com";

    [Range(1, 600)]
    public int TimeoutSeconds { get; set; } = 90;

    [Range(1, 65536)]
    public int MaxOutputTokens { get; set; } = 8192;
}
