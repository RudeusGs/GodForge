using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Admin;

public sealed class DbMaintenanceRun : BaseEntity
{
    public string MaintenanceType { get; private set; } = default!;
    public string? Target { get; private set; }
    public RunStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? DetailsJson { get; private set; }

    private DbMaintenanceRun() { } // EF Core

    public static DbMaintenanceRun Create(
        string maintenanceType, string? target, DateTimeOffset now)
    {
        return new DbMaintenanceRun
        {
            Id = Guid.NewGuid(),
            MaintenanceType = maintenanceType,
            Target = target,
            Status = RunStatus.Running,
            StartedAt = now
        };
    }

    public void MarkAsCompleted(DateTimeOffset now, string? detailsJson = null)
    {
        Status = RunStatus.Completed;
        CompletedAt = now;
        DetailsJson = detailsJson;
    }

    public void MarkAsFailed(DateTimeOffset now, string? detailsJson = null)
    {
        Status = RunStatus.Failed;
        CompletedAt = now;
        DetailsJson = detailsJson;
    }
}
