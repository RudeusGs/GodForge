using GodForge.Domain.Entities.Core;

namespace GodForge.Application.Features.Settings;

public sealed record AnalysisProfileOption(string Key, string Version, bool Enabled, bool Compatible);

// Keys include the immutable definition version. Never repoint an existing key to a new definition.
// The current analyzer has one fixed inventory/resource-check mode and no engine-specific modes.
public static class AnalysisProfileRegistry
{
    public const string DefaultKey = "current-default-v1";
    public static IReadOnlyList<AnalysisProfileOption> For(Project project)
    {
        _ = project; // Engine-version compatibility is not yet part of the current inventory analyzer.
        return [new(DefaultKey, "1", true, true)];
    }

    public static bool CanSelect(Project project, string key)
        => For(project).Any(p => p.Key == key && p.Enabled && p.Compatible);
}
