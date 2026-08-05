using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Core;

public sealed class Organization : BaseAuditableEntity, ISoftDeletable
{
    public string Slug { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public OrganizationStatus Status { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private Organization() { }

    public static Organization Create(string name, string slug, Guid actorId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            throw new ArgumentException("Organization slug is invalid.", nameof(slug));

        return new Organization
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug,
            Status = OrganizationStatus.Active,
            CreatedByUserId = actorId,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string name, string slug, long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status != OrganizationStatus.Active)
            throw new InvalidOperationException("Only active organizations can be updated.");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            throw new ArgumentException("Organization slug is invalid.", nameof(slug));
        Name = name.Trim();
        Slug = slug;
        Version++;
        UpdatedAt = now;
    }

    public void MarkDeleting(long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        Status = OrganizationStatus.Deleting;
        Version++;
        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        Status = OrganizationStatus.Deleted;
        DeletedAt = now;
        Version++;
        UpdatedAt = now;
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new InvalidOperationException("Concurrency version mismatch.");
    }
}
