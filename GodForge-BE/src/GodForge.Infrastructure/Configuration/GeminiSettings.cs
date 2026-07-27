namespace GodForge.Infrastructure.Configuration;

public sealed class GeminiSettings
{
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.5-flash";
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com";
    public int TimeoutSeconds { get; set; } = 90;
    public int MaxOutputTokens { get; set; } = 8192;
}
