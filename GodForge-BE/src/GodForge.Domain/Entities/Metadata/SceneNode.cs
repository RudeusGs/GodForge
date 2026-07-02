using System.Collections.Generic;
using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Metadata;

public sealed class SceneNode : BaseEntity
{
    public Guid SceneId { get; private set; }
    public string NodePath { get; private set; } = default!;
    public string NodeName { get; private set; } = default!;
    public string NodeType { get; private set; } = default!;
    public string ParentPath { get; private set; } = default!;
    public int Depth { get; private set; }
    public int NodeOrder { get; private set; }
    public string? ScriptPath { get; private set; }
    public List<string>? Groups { get; private set; }
    public string? ImportantPropertiesJson { get; private set; }
    public string? PropertiesJson { get; private set; }
    public int WarningCount { get; private set; }

    private SceneNode() { } // EF Core

    public static SceneNode Create(
        Guid sceneId, string nodePath, string nodeName, string nodeType,
        string parentPath, int depth, int nodeOrder, string? scriptPath,
        List<string>? groups, string? importantPropertiesJson,
        string? propertiesJson, int warningCount)
    {
        return new SceneNode
        {
            Id = Guid.NewGuid(),
            SceneId = sceneId,
            NodePath = nodePath,
            NodeName = nodeName,
            NodeType = nodeType,
            ParentPath = parentPath,
            Depth = depth,
            NodeOrder = nodeOrder,
            ScriptPath = scriptPath,
            Groups = groups,
            ImportantPropertiesJson = importantPropertiesJson,
            PropertiesJson = propertiesJson,
            WarningCount = warningCount
        };
    }
}
