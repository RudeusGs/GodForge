using GodForge.Domain.Enums;

namespace GodForge.Application.Features.Settings;

public interface IAiExecutionPolicy
{
    Task<bool> IsAllowedAsync(Guid projectId, CancellationToken ct);
}

public sealed record AiExecutionState(Guid OrganizationId, ProjectStatus ProjectStatus,
    OrganizationStatus OrganizationStatus, bool AiRequested);

public interface IAiExecutionStateReader
{
    Task<AiExecutionState?> ReadAsync(Guid projectId, CancellationToken ct);
}

public sealed class AiExecutionPolicy(IAiExecutionStateReader stateReader, EffectiveSettingsPolicy policy) : IAiExecutionPolicy
{
    public async Task<bool> IsAllowedAsync(Guid projectId, CancellationToken ct)
    {
        var state = await stateReader.ReadAsync(projectId, ct);
        return state is { ProjectStatus: ProjectStatus.Active, OrganizationStatus: OrganizationStatus.Active, AiRequested: true }
            && (await policy.ResolveAiAsync(state.OrganizationId, true, ct)).Value;
    }
}
