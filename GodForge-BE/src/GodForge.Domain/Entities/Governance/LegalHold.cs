using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Governance;

public sealed class LegalHold : BaseEntity
{
    public string TargetType { get; private set; } = default!;
    public Guid TargetId { get; private set; }
    public string Reason { get; private set; } = default!;
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }

    private LegalHold() { } // EF Core

    public static LegalHold Create(
        string targetType, Guid targetId, string reason,
        Guid createdBy, DateTimeOffset now)
    {
        return new LegalHold
        {
            Id = Guid.NewGuid(),
            TargetType = targetType,
            TargetId = targetId,
            Reason = reason,
            CreatedBy = createdBy,
            CreatedAt = now
        };
    }
}
