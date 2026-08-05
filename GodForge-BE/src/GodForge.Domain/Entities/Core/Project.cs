using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Core;

public sealed class Project : BaseAuditableEntity, ISoftDeletable
{
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public string GodotVersion { get; private set; } = default!;
    public ProjectVisibility Visibility { get; private set; }
    public ProjectStatus Status { get; private set; }
    public int? HealthScore { get; private set; }
    public Guid CreatedBy { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private Project() { }

    public static Project Create(Guid organizationId, string name, string slug, string? description, string godotVersion, ProjectVisibility visibility, Guid createdBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            throw new ArgumentException("Project slug is invalid.", nameof(slug));

        return new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name.Trim(),
            Slug = slug,
            Description = description?.Trim(),
            GodotVersion = godotVersion,
            Visibility = visibility,
            Status = ProjectStatus.Active,
            CreatedBy = createdBy,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDetails(string name, string slug, string? description, ProjectVisibility visibility, long expectedVersion, DateTimeOffset now)
    {
        EnsureMutable(expectedVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            throw new ArgumentException("Project slug is invalid.", nameof(slug));
        Name = name.Trim();
        Slug = slug;
        Description = description?.Trim();
        Visibility = visibility;
        Version++;
        UpdatedAt = now;
    }

    public void Archive(long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status is ProjectStatus.Deleted or ProjectStatus.Deleting)
            throw new InvalidOperationException("Deleted projects cannot be archived.");
        Status = ProjectStatus.Archived;
        ArchivedAt = now;
        Version++;
        UpdatedAt = now;
    }

    public void Restore(long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status != ProjectStatus.Archived)
            throw new InvalidOperationException("Only archived projects can be restored.");
        Status = ProjectStatus.Active;
        ArchivedAt = null;
        Version++;
        UpdatedAt = now;
    }


    public void MarkDeleting(long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status == ProjectStatus.Deleted)
            throw new InvalidOperationException("Deleted projects cannot enter deletion workflow.");
        Status = ProjectStatus.Deleting;
        Version++;
        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (DeletedAt is not null) return;
        DeletedAt = now;
        Status = ProjectStatus.Deleted;
        Version++;
        UpdatedAt = now;
    }

    private void EnsureMutable(long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        if (Status != ProjectStatus.Active || DeletedAt is not null)
            throw new InvalidOperationException("Only active projects can be changed.");
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new InvalidOperationException("Concurrency version mismatch.");
    }
}