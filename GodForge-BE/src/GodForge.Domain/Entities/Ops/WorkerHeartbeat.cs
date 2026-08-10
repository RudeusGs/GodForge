using System.Collections.Generic;
using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Ops;

public sealed class WorkerHeartbeat : BaseEntity
{
    public string WorkerName { get; private set; } = default!;
    public string WorkerInstanceId { get; private set; } = default!;
    public List<string> Queues { get; private set; } = new();
    public WorkerHeartbeatStatus Status { get; private set; }
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
            Status = WorkerHeartbeatStatus.Starting,
            StartedAt = now,
            LastSeenAt = now,
            MetadataJson = metadataJson
        };
    }

    public void Heartbeat(WorkerHeartbeatStatus status, DateTimeOffset now)
    {
        EnumGuard.ThrowIfUndefined(status, nameof(status));
        Status = status;
        LastSeenAt = now;
    }
}
