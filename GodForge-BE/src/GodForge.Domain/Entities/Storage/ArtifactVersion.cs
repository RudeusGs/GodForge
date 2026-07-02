using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Storage;

public sealed class ArtifactVersion : BaseEntity
{
    public Guid ArtifactId { get; private set; }
    public int VersionNumber { get; private set; }
    public long Size { get; private set; }
    public string ObjectPath { get; private set; } = default!;
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ArtifactVersion() { } // EF Core

    public static ArtifactVersion Create(Guid artifactId, int versionNumber, long size, string objectPath, Guid createdBy, DateTimeOffset now)
    {
        return new ArtifactVersion
        {
            Id = Guid.NewGuid(),
            ArtifactId = artifactId,
            VersionNumber = versionNumber,
            Size = size,
            ObjectPath = objectPath,
            CreatedBy = createdBy,
            CreatedAt = now
        };
    }
}
