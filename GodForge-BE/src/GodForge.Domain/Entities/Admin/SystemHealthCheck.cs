using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Admin;

public sealed class SystemHealthCheck : BaseEntity
{
    public string Component { get; private set; } = default!;
    public SystemHealthStatus Status { get; private set; }
    public string? DetailsJson { get; private set; }
    public DateTimeOffset CheckedAt { get; private set; }

    private SystemHealthCheck() { } // EF Core

    public static SystemHealthCheck Create(
        string component, SystemHealthStatus status, string? detailsJson, DateTimeOffset checkedAt)
    {
        EnumGuard.ThrowIfUndefined(status, nameof(status));

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
