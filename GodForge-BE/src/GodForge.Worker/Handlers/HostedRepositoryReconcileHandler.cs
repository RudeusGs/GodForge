using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Models.Messages;
using GodForge.Application.Features.Repositories;
using GodForge.Domain.Enums;

namespace GodForge.Worker.Handlers;

public sealed class HostedRepositoryReconcileHandler(M2JobExecution execution, IHostedRepositoryAccessStore store,
    IHostedGitService provider, IClock clock)
{
    private sealed record Input(string ProviderId, long Generation, long? HookId, IReadOnlyDictionary<long, (string Login, string Permission)> Desired);
    private sealed record Verified(long HookId);
    public Task<JobExecutionResult> HandleAsync(HostedRepositoryReconcileJobMessage message, CancellationToken cancellationToken)
        => execution.RunAsync<Input, Verified>(message, JobType.ReconcileHostedRepository, WorkerQueueNames.HostedRepositoryReconcile,
            async (_, token) =>
            {
                var scope = await ReadAsync(message, token);
                return new(scope.Repository.HostedRepositoryId!, scope.Repository.AccessGeneration, scope.Repository.WebhookId, Desired(scope));
            },
            async (input, token) =>
            {
                var actual = await provider.CollaboratorsAsync(input.ProviderId, token);
                foreach (var existing in actual)
                    if (!input.Desired.TryGetValue(existing.Id, out var wanted) || wanted.Permission != existing.Permission)
                        await provider.SetCollaboratorAsync(input.ProviderId, existing.Id, existing.Login, null, token);
                foreach (var member in input.Desired)
                    if (!actual.Any(a => a.Id == member.Key && a.Permission == member.Value.Permission))
                        await provider.SetCollaboratorAsync(input.ProviderId, member.Key, member.Value.Login, member.Value.Permission, token);
                var verified = await provider.CollaboratorsAsync(input.ProviderId, token);
                if (verified.Count != input.Desired.Count || verified.Any(a => !input.Desired.TryGetValue(a.Id, out var d) || d.Permission != a.Permission))
                    throw new HostedGitException("FORGEJO_RECONCILIATION_INCOMPLETE", true);
                return new(await provider.EnsureWebhookAsync(input.ProviderId, input.HookId, token));
            },
            async (_, input, verified, token) =>
            {
                var current = await ReadAsync(message, token);
                var desired = Desired(current);
                if (current.Repository.AccessGeneration != input.Generation || desired.Count != input.Desired.Count ||
                    desired.Any(d => !input.Desired.TryGetValue(d.Key, out var previous) || previous != d.Value))
                {
                    current.Repository.FailAccessReconciliation("RECONCILIATION_PENDING");
                    await execution.QueueReconciliationAsync(current.Repository.Id, message.ProjectId, message.OrganizationId, message.CorrelationId, token);
                    return; // The old job ran, but it does not confirm the current generation.
                }
                current.Repository.CompleteAccessReconciliation(clock.UtcNow);
                current.Repository.ConfirmWebhook(verified.HookId, clock.UtcNow);
            },
            async (code, token) =>
            {
                var scope = await store.ReadAsync(message.RepositoryId!.Value, token);
                if (scope?.Repository.ProjectId == message.ProjectId && scope.OrganizationId == message.OrganizationId) scope.Repository.FailAccessReconciliation(code);
            }, cancellationToken);

    private async Task<HostedAccessScope> ReadAsync(HostedRepositoryReconcileJobMessage message, CancellationToken token)
    {
        var scope = await store.ReadAsync(message.RepositoryId!.Value, token);
        if (scope is null || scope.Repository.ProjectId != message.ProjectId || scope.OrganizationId != message.OrganizationId)
            throw new HostedGitException("WORKER_MESSAGE_INVALID");
        return scope;
    }
    private static Dictionary<long, (string Login, string Permission)> Desired(HostedAccessScope scope)
        => scope.Members.ToDictionary(m => m.ProviderUserId, m => (m.Login, HostedPermissionMapping.For(m.Role)));
}
