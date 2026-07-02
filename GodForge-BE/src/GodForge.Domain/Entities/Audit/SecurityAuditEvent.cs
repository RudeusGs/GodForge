using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Audit;

public sealed class SecurityAuditEvent : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string EventType { get; private set; } = default!;
    public string Severity { get; private set; } = default!;
    public string? DetailsJson { get; private set; }
    public string CorrelationId { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private SecurityAuditEvent() { } // EF Core

    public static SecurityAuditEvent Create(
        Guid? userId, string eventType, string severity,
        string? detailsJson, string correlationId, DateTimeOffset now)
    {
        return new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = eventType,
            Severity = severity,
            DetailsJson = detailsJson,
            CorrelationId = correlationId,
            CreatedAt = now
        };
    }
}
