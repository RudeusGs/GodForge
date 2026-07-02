using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Metadata;

public sealed class SceneNodeProperty : BaseEntity
{
    public Guid SceneNodeId { get; private set; }
    public string PropertyName { get; private set; } = default!;
    public string? PropertyType { get; private set; }
    public string? PropertyValueJson { get; private set; }
    public string? PropertyValueHash { get; private set; }

    private SceneNodeProperty() { } // EF Core

    public static SceneNodeProperty Create(
        Guid sceneNodeId, string propertyName, string? propertyType,
        string? propertyValueJson, string? propertyValueHash)
    {
        return new SceneNodeProperty
        {
            Id = Guid.NewGuid(),
            SceneNodeId = sceneNodeId,
            PropertyName = propertyName,
            PropertyType = propertyType,
            PropertyValueJson = propertyValueJson,
            PropertyValueHash = propertyValueHash
        };
    }
}
