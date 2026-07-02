using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Admin;

public sealed class SystemHealthCheck : BaseEntity
{
    public string Component { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public string? DetailsJson { get; private set; }
    public DateTimeOffset CheckedAt { get; private set; }

    private SystemHealthCheck() { } // EF Core

    public static SystemHealthCheck Create(
        string component, string status, string? detailsJson, DateTimeOffset checkedAt)
    {
        return new SystemHealthCheck
        {
            Id = Guid.NewGuid(),
            Component = component,
            Status = status,
            DetailsJson = detailsJson,
            CheckedAt = checkedAt
        };
    }
}
