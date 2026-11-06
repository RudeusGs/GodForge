using GodForge.Application.Common.Models;
using GodForge.Domain.Entities.Repo;

namespace GodForge.Application.Common.Interfaces;

public interface IHostedWebhookStore
{
    Task<(GitRepository Repository, Guid OrganizationId)?> ResolveAsync(string providerId, CancellationToken cancellationToken);
    Task<bool> DeliveryExistsAsync(string deliveryId, CancellationToken cancellationToken);
    Task RecordAsync(WebhookEvent webhook, CancellationToken cancellationToken);
    Task DeleteReferenceAsync(Guid repositoryId, string branch, CancellationToken cancellationToken);
    Task RecordSyncRunAsync(Guid repositoryId, DateTimeOffset started, DateTimeOffset completed, string? errorCode, CancellationToken token);
    Task SaveRevisionAsync(Guid repositoryId, string branch, RepositoryCommitDto commit, DateTimeOffset now, CancellationToken cancellationToken);
}
public interface IForgejoWebhookSignature
{
    bool Verify(byte[] body, string signature);
}
