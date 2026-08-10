using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Repo;

public sealed class RepositorySyncRun : BaseEntity
{
    public Guid RepositoryId { get; private set; }
    public string Type { get; private set; } = default!;
    public RunStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private RepositorySyncRun() { } // EF Core

    public static RepositorySyncRun Create(Guid repositoryId, string type, DateTimeOffset now)
    {
        return new RepositorySyncRun
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            Type = type,
            Status = RunStatus.Running,
            StartedAt = now,
            CreatedAt = now
        };
    }

    public void MarkAsCompleted(DateTimeOffset now)
    {
        Status = RunStatus.Completed;
        CompletedAt = now;
    }

    public void MarkAsFailed(string errorMessage, DateTimeOffset now)
    {
        Status = RunStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = now;
    }
}
