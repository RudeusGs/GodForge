using GodForge.Application.Features.Settings;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Configuration;

public sealed class PlatformCapabilities(IOptionsMonitor<GeminiSettings> settings) : IPlatformCapabilities
{
    public bool AiAdvisoryAllowed => settings.CurrentValue.Enabled;
}
