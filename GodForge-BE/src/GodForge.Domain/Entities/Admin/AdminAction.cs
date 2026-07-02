using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Admin;

public sealed class AdminAction : BaseEntity
{
    public Guid AdminUserId { get; private set; }
    public string TargetType { get; private set; } = default!;
    public Guid? TargetId { get; private set; }
    public string Action { get; private set; } = default!;
    public string Reason { get; private set; } = default!;
    public string Outcome { get; private set; } = default!;
    public string CorrelationId { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private AdminAction() { } // EF Core

    public static AdminAction Create(
        Guid adminUserId, string targetType, Guid? targetId,
        string action, string reason, string outcome,
        string correlationId, DateTimeOffset now)
    {
        return new AdminAction
        {
            Id = Guid.NewGuid(),
            AdminUserId = adminUserId,
            TargetType = targetType,
            TargetId = targetId,
            Action = action,
            Reason = reason,
            Outcome = outcome,
            CorrelationId = correlationId,
            CreatedAt = now
        };
    }
}
