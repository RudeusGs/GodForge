using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Governance;

public sealed class RetentionRun : BaseEntity
{
    public Guid PolicyId { get; private set; }
    public RunStatus Status { get; private set; }
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
            Status = RunStatus.Running,
            AffectedCount = 0,
            StartedAt = now
        };
    }

    public void MarkAsCompleted(int affectedCount, DateTimeOffset now)
    {
        Status = RunStatus.Completed;
        AffectedCount = affectedCount;
        CompletedAt = now;
    }

    public void MarkAsFailed(DateTimeOffset now)
    {
        Status = RunStatus.Failed;
        CompletedAt = now;
    }
}
