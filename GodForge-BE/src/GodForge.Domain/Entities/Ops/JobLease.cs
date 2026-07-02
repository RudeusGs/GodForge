using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Ops;

public sealed class JobLease : BaseEntity
{
    public Guid JobId { get; private set; }
    public string WorkerInstanceId { get; private set; } = default!;
    public string LeaseToken { get; private set; } = default!;
    public DateTimeOffset LeasedUntil { get; private set; }
    public DateTimeOffset AcquiredAt { get; private set; }
    public DateTimeOffset? RenewedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }

    private JobLease() { } // EF Core

    public static JobLease Create(Guid jobId, string workerInstanceId, string leaseToken, DateTimeOffset leasedUntil, DateTimeOffset now)
    {
        return new JobLease
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            WorkerInstanceId = workerInstanceId,
            LeaseToken = leaseToken,
            LeasedUntil = leasedUntil,
            AcquiredAt = now
        };
    }

    public void Renew(DateTimeOffset newLeasedUntil, DateTimeOffset now)
    {
        LeasedUntil = newLeasedUntil;
        RenewedAt = now;
    }

    public void Release(DateTimeOffset now)
    {
        if (ReleasedAt is null)
        {
            ReleasedAt = now;
        }
    }
}
