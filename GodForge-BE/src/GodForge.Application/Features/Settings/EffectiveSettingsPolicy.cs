using GodForge.Application.Common.Interfaces.Repositories;

namespace GodForge.Application.Features.Settings;

public interface IPlatformCapabilities
{
    bool AiAdvisoryAllowed { get; }
}

public sealed record EffectiveBooleanSetting(bool Value, string SourceScope, bool Editable, string? RestrictionReason);

public sealed class EffectiveSettingsPolicy(IPlatformCapabilities platform, IOrganizationSettingRepository organizations)
{
    public async Task<EffectiveBooleanSetting> ResolveAiAsync(Guid organizationId, bool requested, CancellationToken ct)
    {
        if (!platform.AiAdvisoryAllowed)
            return new(false, "platform", false, "AI advisory is disabled by the platform.");
        var allowed = await organizations.GetAiAllowedAsync(organizationId, ct);
        return !allowed
            ? new(false, "organization", false, "AI advisory is disabled by organization policy.")
            : new(requested, "project", true, null);
    }
}
