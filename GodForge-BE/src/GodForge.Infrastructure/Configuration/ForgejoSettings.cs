using System.ComponentModel.DataAnnotations;

namespace GodForge.Infrastructure.Configuration;

public sealed class ForgejoSettings
{
    public bool Enabled { get; set; }

    [MaxLength(500)]
    public string BaseUrl { get; set; } = "http://localhost:3000";

    [MaxLength(500)]
    public string PublicUrl { get; set; } = "http://localhost:3000";

    public bool AllowLocalHttp { get; set; }
    [MaxLength(800)]
    public string WebhookUrl { get; set; } = "";
    public bool HasSafeWebhook => Uri.TryCreate(WebhookUrl, UriKind.Absolute, out var uri) &&
        string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) &&
        uri.AbsolutePath == "/api/v1/webhooks/forgejo" &&
        (uri.Scheme == Uri.UriSchemeHttps || AllowLocalHttp && uri.Scheme == Uri.UriSchemeHttp &&
            (uri.IsLoopback || string.Equals(uri.Host, "api", StringComparison.OrdinalIgnoreCase)));

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 30;

    public bool HasSafeEndpoints => SafeEndpoint(BaseUrl) && SafeEndpoint(PublicUrl);

    private bool SafeEndpoint(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) && uri.AbsolutePath == "/" &&
        (uri.Scheme == Uri.UriSchemeHttps || AllowLocalHttp && uri.Scheme == Uri.UriSchemeHttp &&
            (uri.IsLoopback || string.Equals(uri.Host, "forgejo", StringComparison.OrdinalIgnoreCase)));

    [MaxLength(1000)]
    public string ApiToken { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string WebhookSecret { get; set; } = string.Empty;
}
