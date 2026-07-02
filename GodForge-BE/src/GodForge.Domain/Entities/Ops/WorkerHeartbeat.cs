using System.Collections.Generic;
using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Ops;

public sealed class WorkerHeartbeat : BaseEntity
{
    public string WorkerName { get; private set; } = default!;
    public string WorkerInstanceId { get; private set; } = default!;
    public List<string> Queues { get; private set; } = new();
    public string Status { get; private set; } = default!;
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }
    public string? MetadataJson { get; private set; }

    private WorkerHeartbeat() { } // EF Core

    public static WorkerHeartbeat Create(string workerName, string workerInstanceId, List<string> queues, string? metadataJson, DateTimeOffset now)
    {
        return new WorkerHeartbeat
        {
            Id = Guid.NewGuid(),
            WorkerName = workerName,
            WorkerInstanceId = workerInstanceId,
            Queues = queues,
            Status = "starting",
            StartedAt = now,
            LastSeenAt = now,
            MetadataJson = metadataJson
        };
    }

    public void Heartbeat(string status, DateTimeOffset now)
    {
        Status = status;
        LastSeenAt = now;
    }
}
