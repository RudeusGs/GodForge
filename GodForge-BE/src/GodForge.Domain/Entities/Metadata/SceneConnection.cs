using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Metadata;

public sealed class SceneConnection : BaseEntity
{
    public Guid SceneId { get; private set; }
    public string SignalName { get; private set; } = default!;
    public string FromNodePath { get; private set; } = default!;
    public string ToNodePath { get; private set; } = default!;
    public string MethodName { get; private set; } = default!;
    public int? Flags { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SceneConnection() { } // EF Core

    public static SceneConnection Create(
        Guid sceneId, string signalName, string fromNodePath,
        string toNodePath, string methodName, int? flags, DateTimeOffset now)
    {
        return new SceneConnection
        {
            Id = Guid.NewGuid(),
            SceneId = sceneId,
            SignalName = signalName,
            FromNodePath = fromNodePath,
            ToNodePath = toNodePath,
            MethodName = methodName,
            Flags = flags,
            CreatedAt = now
        };
    }
}
