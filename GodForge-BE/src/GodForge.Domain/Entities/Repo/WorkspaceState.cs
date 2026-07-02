using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Repo;

public sealed class WorkspaceState : BaseAuditableEntity
{
    public Guid RepositoryId { get; private set; }
    public string HeadCommit { get; private set; } = default!;
    public string BranchName { get; private set; } = default!;
    public bool IsLocked { get; private set; }
    public string? LockedBy { get; private set; }
    public DateTimeOffset? LockedAt { get; private set; }

    private WorkspaceState() { } // EF Core

    public static WorkspaceState Create(Guid repositoryId, string headCommit, string branchName, DateTimeOffset now)
    {
        return new WorkspaceState
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            HeadCommit = headCommit,
            BranchName = branchName,
            IsLocked = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Lock(string lockedBy, DateTimeOffset now)
    {
        if (!IsLocked)
        {
            IsLocked = true;
            LockedBy = lockedBy;
            LockedAt = now;
            UpdatedAt = now;
        }
    }

    public void Unlock(DateTimeOffset now)
    {
        if (IsLocked)
        {
            IsLocked = false;
            LockedBy = null;
            LockedAt = null;
            UpdatedAt = now;
        }
    }
}
