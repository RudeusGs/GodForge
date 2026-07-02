using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Audit;

public sealed class AuditLog : BaseEntity
{
    public Guid? ProjectId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string EventType { get; private set; } = default!;
    public string? ResourceType { get; private set; }
    public Guid? ResourceId { get; private set; }
    public string Outcome { get; private set; } = default!;
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string CorrelationId { get; private set; } = default!;
    public string? DetailsJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AuditLog() { } // EF Core

    public static AuditLog Create(
        Guid? projectId, Guid? actorUserId, string eventType,
        string? resourceType, Guid? resourceId, string outcome,
        string? ipAddress, string? userAgent, string correlationId,
        string? detailsJson, DateTimeOffset now)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ActorUserId = actorUserId,
            EventType = eventType,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Outcome = outcome,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CorrelationId = correlationId,
            DetailsJson = detailsJson,
            CreatedAt = now
        };
    }
}
