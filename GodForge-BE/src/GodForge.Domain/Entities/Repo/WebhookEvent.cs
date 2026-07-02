using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Repo;

public sealed class WebhookEvent : BaseEntity
{
    public Guid RepositoryId { get; private set; }
    public string Provider { get; private set; } = default!;
    public string EventType { get; private set; } = default!;
    public string DeliveryId { get; private set; } = default!;
    public string PayloadJson { get; private set; } = default!;
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private WebhookEvent() { } // EF Core

    public static WebhookEvent Create(Guid repositoryId, string provider, string eventType, string deliveryId, string payloadJson, DateTimeOffset now)
    {
        return new WebhookEvent
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            Provider = provider,
            EventType = eventType,
            DeliveryId = deliveryId,
            PayloadJson = payloadJson,
            CreatedAt = now
        };
    }

    public void MarkAsProcessed(DateTimeOffset now)
    {
        if (ProcessedAt is null)
        {
            ProcessedAt = now;
        }
    }
}
