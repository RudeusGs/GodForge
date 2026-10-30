using System.Diagnostics.Metrics;

namespace GodForge.Application.Features.Settings;

public static class SettingsMetrics
{
    private static readonly Meter Meter = new("GodForge.Settings");
    private static readonly Counter<long> Updates = Meter.CreateCounter<long>("godforge.settings.updates");
    public static void Updated(string scope) => Updates.Add(1, new KeyValuePair<string, object?>("scope", scope), new KeyValuePair<string, object?>("outcome", "succeeded"));
}
