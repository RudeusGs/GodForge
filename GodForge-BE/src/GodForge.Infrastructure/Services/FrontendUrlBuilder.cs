using GodForge.Application.Common.Interfaces;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Services;

public sealed class FrontendUrlBuilder : IFrontendUrlBuilder
{
    private readonly FrontendSettings _settings;

    public FrontendUrlBuilder(IOptions<FrontendSettings> settings)
    {
        _settings = settings.Value;
    }

    public string BuildPasswordResetUrl(string email, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
    }
}
