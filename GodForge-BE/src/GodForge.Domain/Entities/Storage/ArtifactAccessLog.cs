using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Storage;

public sealed class ArtifactAccessLog : BaseEntity
{
    public Guid ArtifactId { get; private set; }
    public Guid UserId { get; private set; }
    public string? IpAddress { get; private set; }
    public string Action { get; private set; } = default!;
    public DateTimeOffset AccessedAt { get; private set; }

    private ArtifactAccessLog() { } // EF Core

    public static ArtifactAccessLog Create(Guid artifactId, Guid userId, string? ipAddress, string action, DateTimeOffset now)
    {
        return new ArtifactAccessLog
        {
            Id = Guid.NewGuid(),
            ArtifactId = artifactId,
            UserId = userId,
            IpAddress = ipAddress,
            Action = action,
            AccessedAt = now
        };
    }
}
