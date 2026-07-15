namespace GodForge.Application.Common.Models.Messages;

public abstract record WorkerMessage
{
    public string SchemaVersion { get; init; } = "1.0";
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public Guid JobId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid? RepositoryId { get; init; }
    public string CorrelationId { get; init; } = default!;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public int AttemptCount { get; init; }
    public string? InputHash { get; init; }
}

public sealed record RepositoryAnalysisJobMessage : WorkerMessage
{
    public string Branch { get; init; } = "main";
    public string AnalysisProfile { get; init; } = "health_overview";
    public bool IncludeAi { get; init; }
}

public sealed record HostedRepositoryProvisionJobMessage : WorkerMessage
{
    public string Owner { get; init; } = default!;
    public string Name { get; init; } = default!;
    public bool IsPrivate { get; init; } = true;
    public string DefaultBranch { get; init; } = "main";
}
