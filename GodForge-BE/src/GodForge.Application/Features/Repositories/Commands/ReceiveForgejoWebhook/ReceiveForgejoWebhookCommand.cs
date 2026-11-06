using System.Globalization;
using System.Text.Json;
using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Models.Messages;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Repositories.Commands.ReceiveForgejoWebhook;

public sealed record ReceiveForgejoWebhookCommand(byte[] Body, string Signature, string DeliveryId, string EventType, string CorrelationId) : IRequest<Result<object>>;
public sealed class ReceiveForgejoWebhookCommandHandler(IForgejoWebhookSignature signature, IHostedWebhookStore store,
    IUnitOfWork unitOfWork, IJobRepository jobs, IOutboxWriter outbox, IClock clock) : IRequestHandler<ReceiveForgejoWebhookCommand, Result<object>>
{
    public async Task<Result<object>> Handle(ReceiveForgejoWebhookCommand request, CancellationToken cancellationToken)
    {
        if (!signature.Verify(request.Body, request.Signature)) return ApplicationError.Unauthorized("WEBHOOK_SIGNATURE_INVALID", "Webhook authentication failed.");
        if (!Guid.TryParse(request.DeliveryId, out var delivery) || delivery == Guid.Empty || request.Body.Length > 1048576 || request.EventType.Length > 80)
            return ApplicationError.Validation("WEBHOOK_PAYLOAD_INVALID", "Webhook metadata is invalid.");
        try
        {
            using var payload = JsonDocument.Parse(request.Body, new JsonDocumentOptions { MaxDepth = 32 });
            var root = payload.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("repository", out var providerRepository) ||
                providerRepository.ValueKind != JsonValueKind.Object || !providerRepository.TryGetProperty("id", out var id) ||
                id.ValueKind != JsonValueKind.Number || !id.TryGetInt64(out var providerNumericId) || providerNumericId <= 0)
                throw new JsonException();
            var providerId = providerNumericId.ToString(CultureInfo.InvariantCulture);
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var scope = await store.ResolveAsync(providerId, cancellationToken);
                if (scope is null) return ApplicationError.NotFound("REPOSITORY_NOT_CONNECTED", "Repository mapping was not found.");
                await unitOfWork.AcquireResourceLockAsync("organization-membership", scope.Value.OrganizationId, cancellationToken);
                await unitOfWork.AcquireResourceLockAsync("project-membership", scope.Value.Repository.ProjectId, cancellationToken);
                // Delivery IDs are global to the provider, including hostile cross-repository reuse.
                await unitOfWork.AcquireResourceLockAsync("forgejo-delivery", delivery, cancellationToken);
                unitOfWork.ClearTrackedChanges();
                scope = await store.ResolveAsync(providerId, cancellationToken);
                if (scope is null) return ApplicationError.NotFound("REPOSITORY_NOT_CONNECTED", "Repository mapping was not found.");
                if (await store.DeliveryExistsAsync(delivery.ToString("D"), cancellationToken)) return Result<object>.Success(new { duplicate = true });
                var repository = scope.Value.Repository;
                var now = clock.UtcNow;
                if (request.EventType is not ("push" or "repository" or "create" or "delete")) return Result<object>.Success(new { ignored = true });
                if (request.EventType is "create" or "delete" && ReadString(root, "ref_type") != "branch") return Result<object>.Success(new { ignored = true });
                var reference = request.EventType == "push" ? ReadString(root, "ref")
                    : "refs/heads/" + (request.EventType == "repository" ? ReadString(providerRepository, "default_branch") : ReadString(root, "ref"));
                var sha = request.EventType == "push" ? ReadString(root, "after") : new string('0', 40);
                if (!reference.StartsWith("refs/heads/", StringComparison.Ordinal) || !RepositoryInput.ValidBranch(reference[11..]) || !RepositoryInput.ValidSha(sha))
                    return ApplicationError.Validation("WEBHOOK_PAYLOAD_INVALID", "Webhook revision is invalid.");
                var branch = reference[11..];
                var deletion = request.EventType == "delete" || request.EventType == "push" && sha.All(c => c == '0');
                var key = $"push:{repository.Id:N}:{branch}:{(sha.All(c => c == '0') ? delivery.ToString("N") : sha)}";
                // Hash the branch to keep durable keys bounded for long names.
                key = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key)));
                var job = await jobs.GetByIdempotencyKeyAsync(repository.ProjectId, JobType.FetchRepository, key, cancellationToken);
                if (job is null)
                {
                    job = Job.Create(repository.ProjectId, repository.Id, JobType.FetchRepository, WorkerQueueNames.RepositorySync,
                        0, null, key, 3, null, request.CorrelationId, now, now);
                    await jobs.AddAsync(job, cancellationToken);
                    await outbox.EnqueueAsync(WorkerQueueNames.RepositorySync, new RepositorySyncJobMessage
                    {
                        JobId = job.Id,
                        ProjectId = repository.ProjectId,
                        RepositoryId = repository.Id,
                        OrganizationId = scope.Value.OrganizationId,
                        Branch = branch,
                        IsDeletion = deletion,
                        CorrelationId = request.CorrelationId,
                        InputHash = key,
                        CreatedAt = now
                    }, cancellationToken);
                }
                var webhook = WebhookEvent.Create(repository.Id, "Forgejo", request.EventType, delivery.ToString("D"), JsonSerializer.Serialize(new { reference, sha }), now);
                webhook.MarkAsProcessed(now); // Processing means durable handoff, never worker completion.
                await store.RecordAsync(webhook, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken);
                return Result<object>.Success(new { jobId = job.Id });
            }
            finally { await unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
        }
        catch (JsonException)
        { return ApplicationError.Validation("WEBHOOK_PAYLOAD_INVALID", "Webhook payload is invalid."); }
    }

    private static string ReadString(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { } text
            ? text : throw new JsonException();
}
