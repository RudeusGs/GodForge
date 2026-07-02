using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Ops;

public sealed class JobEvent : BaseEntity
{
    public Guid JobId { get; private set; }
    public string EventType { get; private set; } = default!;
    public int? Progress { get; private set; }
    public string? Message { get; private set; }
    public string? DataJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private JobEvent() { } // EF Core

    public static JobEvent Create(Guid jobId, string eventType, int? progress, string? message, string? dataJson, DateTimeOffset now)
    {
        return new JobEvent
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            EventType = eventType,
            Progress = progress,
            Message = message,
            DataJson = dataJson,
            CreatedAt = now
        };
    }
}
