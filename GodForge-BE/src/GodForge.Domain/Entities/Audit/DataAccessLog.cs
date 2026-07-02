using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Audit;

public sealed class DataAccessLog : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string ResourceType { get; private set; } = default!;
    public Guid? ResourceId { get; private set; }
    public string Action { get; private set; } = default!;
    public string Outcome { get; private set; } = default!;
    public string? IpAddress { get; private set; }
    public string CorrelationId { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private DataAccessLog() { } // EF Core

    public static DataAccessLog Create(
        Guid? userId, string resourceType, Guid? resourceId,
        string action, string outcome, string? ipAddress,
        string correlationId, DateTimeOffset now)
    {
        return new DataAccessLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Action = action,
            Outcome = outcome,
            IpAddress = ipAddress,
            CorrelationId = correlationId,
            CreatedAt = now
        };
    }
}
