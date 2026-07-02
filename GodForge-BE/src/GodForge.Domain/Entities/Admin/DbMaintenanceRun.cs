using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Admin;

public sealed class DbMaintenanceRun : BaseEntity
{
    public string MaintenanceType { get; private set; } = default!;
    public string? Target { get; private set; }
    public string Status { get; private set; } = default!;
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
            Status = "running",
            StartedAt = now
        };
    }

    public void MarkAsCompleted(DateTimeOffset now, string? detailsJson = null)
    {
        Status = "completed";
        CompletedAt = now;
        DetailsJson = detailsJson;
    }

    public void MarkAsFailed(DateTimeOffset now, string? detailsJson = null)
    {
        Status = "failed";
        CompletedAt = now;
        DetailsJson = detailsJson;
    }
}
