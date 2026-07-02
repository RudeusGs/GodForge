using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Governance;

public sealed class RetentionRun : BaseEntity
{
    public Guid PolicyId { get; private set; }
    public string Status { get; private set; } = default!;
    public int AffectedCount { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private RetentionRun() { } // EF Core

    public static RetentionRun Create(
        Guid policyId, DateTimeOffset now)
    {
        return new RetentionRun
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            Status = "running",
            AffectedCount = 0,
            StartedAt = now
        };
    }

    public void MarkAsCompleted(int affectedCount, DateTimeOffset now)
    {
        Status = "completed";
        AffectedCount = affectedCount;
        CompletedAt = now;
    }

    public void MarkAsFailed(DateTimeOffset now)
    {
        Status = "failed";
        CompletedAt = now;
    }
}
