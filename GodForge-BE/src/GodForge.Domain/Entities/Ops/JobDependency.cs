using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Ops;

public sealed class JobDependency : BaseEntity
{
    public Guid JobId { get; private set; }
    public Guid DependsOnJobId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private JobDependency() { } // EF Core

    public static JobDependency Create(Guid jobId, Guid dependsOnJobId, DateTimeOffset now)
    {
        return new JobDependency
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            DependsOnJobId = dependsOnJobId,
            CreatedAt = now
        };
    }
}
