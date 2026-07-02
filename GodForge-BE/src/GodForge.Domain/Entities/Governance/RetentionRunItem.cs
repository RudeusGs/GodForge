using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Governance;

public sealed class RetentionRunItem : BaseEntity
{
    public Guid RetentionRunId { get; private set; }
    public string TargetTable { get; private set; } = default!;
    public Guid? TargetId { get; private set; }
    public string Action { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public string? ErrorMessage { get; private set; }

    private RetentionRunItem() { } // EF Core

    public static RetentionRunItem Create(
        Guid retentionRunId, string targetTable, Guid? targetId,
        string action, string status, string? errorMessage)
    {
        return new RetentionRunItem
        {
            Id = Guid.NewGuid(),
            RetentionRunId = retentionRunId,
            TargetTable = targetTable,
            TargetId = targetId,
            Action = action,
            Status = status,
            ErrorMessage = errorMessage
        };
    }
}
