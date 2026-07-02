using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Identity;

public sealed class SecurityEvent : BaseEntity
{
    public Guid UserId { get; private set; }
    public string EventType { get; private set; } = default!;
    public string? IpAddress { get; private set; }
    public string? DeviceName { get; private set; }
    public string? MetadataJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SecurityEvent() { } // EF Core

    public static SecurityEvent Create(Guid userId, string eventType, string? ipAddress, string? deviceName, string? metadataJson, DateTimeOffset now)
    {
        return new SecurityEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = eventType,
            IpAddress = ipAddress,
            DeviceName = deviceName,
            MetadataJson = metadataJson,
            CreatedAt = now
        };
    }
}
