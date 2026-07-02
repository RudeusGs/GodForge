namespace GodForge.Application.Common.Models.Messages;

public abstract record WorkerMessage
{
    public string SchemaVersion { get; init; } = "1.0";
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public Guid JobId { get; init; }
    public Guid ProjectId { get; init; }
    public string CorrelationId { get; init; } = default!;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public int AttemptCount { get; init; }
}

public record CloneJobMessage(Guid RepositoryId, string RemoteUrl, string? Branch) : WorkerMessage;

public record FetchJobMessage(Guid RepositoryId, string RemoteUrl) : WorkerMessage;

public record ParseJobMessage(Guid RepositoryId, Guid SnapshotId, string CommitHash) : WorkerMessage;

public record AnalyzeJobMessage(Guid RepositoryId, Guid SnapshotId, string CommitHash) : WorkerMessage;

public record DiffJobMessage(Guid RepositoryId, Guid BaseSnapshotId, Guid HeadSnapshotId) : WorkerMessage;

public record PreviewJobMessage(Guid AssetId, string StorageKey) : WorkerMessage;

public record NotificationJobMessage(Guid UserId, string NotificationType, Guid EntityId) : WorkerMessage;
