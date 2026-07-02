using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Storage;

public sealed class Artifact : BaseAuditableEntity
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Type { get; private set; } = default!;
    public long Size { get; private set; }
    public string ObjectPath { get; private set; } = default!;
    public string? MimeType { get; private set; }
    public Guid CreatedBy { get; private set; }

    private Artifact() { } // EF Core

    public static Artifact Create(Guid projectId, string name, string type, long size, string objectPath, string? mimeType, Guid createdBy, DateTimeOffset now)
    {
        return new Artifact
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name,
            Type = type,
            Size = size,
            ObjectPath = objectPath,
            MimeType = mimeType,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdatePath(string newObjectPath, long newSize, DateTimeOffset now)
    {
        ObjectPath = newObjectPath;
        Size = newSize;
        UpdatedAt = now;
    }
}
