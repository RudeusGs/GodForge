namespace GodForge.Infrastructure.Configuration;

public sealed class ForgejoSettings
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "http://localhost:3000";
    public string ApiToken { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}
