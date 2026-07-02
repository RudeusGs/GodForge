using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Core;

public sealed class ProjectSetting : BaseAuditableEntity
{
    public Guid ProjectId { get; private set; }
    public string DefaultRole { get; private set; } = default!;
    public string Visibility { get; private set; } = default!;
    public string? FeaturesJson { get; private set; }

    private ProjectSetting() { } // EF Core

    public static ProjectSetting Create(Guid projectId, DateTimeOffset now)
    {
        return new ProjectSetting
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            DefaultRole = "viewer",
            Visibility = "private",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string defaultRole, string visibility, string? featuresJson, DateTimeOffset now)
    {
        DefaultRole = defaultRole;
        Visibility = visibility;
        FeaturesJson = featuresJson;
        UpdatedAt = now;
    }
}
