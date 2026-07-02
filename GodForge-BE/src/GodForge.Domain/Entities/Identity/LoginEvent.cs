using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Identity;

public sealed class LoginEvent : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? DeviceName { get; private set; }
    public string? UserAgent { get; private set; }
    public string Status { get; private set; } = default!;
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private LoginEvent() { } // EF Core

    public static LoginEvent Create(Guid? userId, string? ipAddress, string? deviceName, string? userAgent, string status, string? failureReason, DateTimeOffset now)
    {
        return new LoginEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IpAddress = ipAddress,
            DeviceName = deviceName,
            UserAgent = userAgent,
            Status = status,
            FailureReason = failureReason,
            CreatedAt = now
        };
    }
}
