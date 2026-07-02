using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Admin;

public sealed class MigrationRun : BaseEntity
{
    public string MigrationName { get; private set; } = default!;
    public string MigrationVersion { get; private set; } = default!;
    public string? Checksum { get; private set; }
    public string Status { get; private set; } = default!;
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? ExecutedBy { get; private set; }
    public string? ErrorMessage { get; private set; }

    private MigrationRun() { } // EF Core

    public static MigrationRun Create(
        string migrationName, string migrationVersion, string? checksum,
        DateTimeOffset startedAt, string? executedBy)
    {
        return new MigrationRun
        {
            Id = Guid.NewGuid(),
            MigrationName = migrationName,
            MigrationVersion = migrationVersion,
            Checksum = checksum,
            Status = "running",
            StartedAt = startedAt,
            ExecutedBy = executedBy
        };
    }

    public void MarkAsCompleted(DateTimeOffset now)
    {
        Status = "completed";
        CompletedAt = now;
    }

    public void MarkAsFailed(string error, DateTimeOffset now)
    {
        Status = "failed";
        ErrorMessage = error;
        CompletedAt = now;
    }
}
