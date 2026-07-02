using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Repo;

public sealed class RepositorySyncRun : BaseEntity
{
    public Guid RepositoryId { get; private set; }
    public string Type { get; private set; } = default!;
    public string Status { get; private set; } = default!;
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
            Status = "running",
            StartedAt = now,
            CreatedAt = now
        };
    }

    public void MarkAsCompleted(DateTimeOffset now)
    {
        Status = "completed";
        CompletedAt = now;
    }

    public void MarkAsFailed(string errorMessage, DateTimeOffset now)
    {
        Status = "failed";
        ErrorMessage = errorMessage;
        CompletedAt = now;
    }
}
