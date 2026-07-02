using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Core;

public sealed class Project : BaseAuditableEntity, ISoftDeletable
{
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public string GodotVersion { get; private set; } = default!;
    public ProjectVisibility Visibility { get; private set; }
    public ProjectStatus Status { get; private set; }
    public int? HealthScore { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private Project() { }

    public static Project Create(string name, string slug, string? description, string godotVersion, ProjectVisibility visibility, Guid createdBy, DateTimeOffset now)
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug,
            Description = description,
            GodotVersion = godotVersion,
            Visibility = visibility,
            Status = ProjectStatus.Active,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDetails(string name, string? description, DateTimeOffset now)
    {
        if (DeletedAt is not null)
            throw new InvalidOperationException("Deleted projects cannot be updated.");

        Name = name.Trim();
        Description = description;
        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (DeletedAt is not null) return;
        DeletedAt = now;
        Status = ProjectStatus.Deleted;
        UpdatedAt = now;
    }
}
