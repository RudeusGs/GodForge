using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Metadata;

public sealed class SceneNodeReference : BaseEntity
{
    public Guid SceneNodeId { get; private set; }
    public string PropertyName { get; private set; } = default!;
    public string ReferenceType { get; private set; } = default!;
    public string? TargetPath { get; private set; }
    public string? TargetUid { get; private set; }
    public bool TargetExists { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SceneNodeReference() { } // EF Core

    public static SceneNodeReference Create(
        Guid sceneNodeId, string propertyName, string referenceType,
        string? targetPath, string? targetUid, bool targetExists, DateTimeOffset now)
    {
        return new SceneNodeReference
        {
            Id = Guid.NewGuid(),
            SceneNodeId = sceneNodeId,
            PropertyName = propertyName,
            ReferenceType = referenceType,
            TargetPath = targetPath,
            TargetUid = targetUid,
            TargetExists = targetExists,
            CreatedAt = now
        };
    }
}
