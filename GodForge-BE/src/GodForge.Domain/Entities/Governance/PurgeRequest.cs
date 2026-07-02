using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Governance;

public sealed class PurgeRequest : BaseEntity
{
    public string TargetType { get; private set; } = default!;
    public Guid TargetId { get; private set; }
    public string Reason { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public Guid RequestedBy { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private PurgeRequest() { } // EF Core

    public static PurgeRequest Create(
        string targetType, Guid targetId, string reason,
        Guid requestedBy, DateTimeOffset now)
    {
        return new PurgeRequest
        {
            Id = Guid.NewGuid(),
            TargetType = targetType,
            TargetId = targetId,
            Reason = reason,
            Status = "pending",
            RequestedBy = requestedBy,
            CreatedAt = now
        };
    }
}
