using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Ops;

public sealed class JobCancellation : BaseEntity
{
    public Guid JobId { get; private set; }
    public Guid RequestedBy { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private JobCancellation() { } // EF Core

    public static JobCancellation Create(Guid jobId, Guid requestedBy, string? reason, DateTimeOffset now)
    {
        return new JobCancellation
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            RequestedBy = requestedBy,
            Reason = reason,
            CreatedAt = now
        };
    }
}
