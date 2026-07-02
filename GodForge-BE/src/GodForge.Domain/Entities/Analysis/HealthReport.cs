using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Analysis;

public sealed class HealthReport : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid RepositoryId { get; private set; }
    public Guid SnapshotId { get; private set; }
    public Guid AnalysisRunId { get; private set; }
    public Guid? JobId { get; private set; }
    public int Score { get; private set; }
    public int TotalIssues { get; private set; }
    public int CriticalCount { get; private set; }
    public int WarningCount { get; private set; }
    public int InfoCount { get; private set; }
    public string? SummaryJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private HealthReport() { } // EF Core

    public static HealthReport Create(
        Guid projectId, Guid repositoryId, Guid snapshotId,
        Guid analysisRunId, Guid? jobId, int score,
        int totalIssues, int criticalCount, int warningCount,
        int infoCount, string? summaryJson, DateTimeOffset now)
    {
        return new HealthReport
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RepositoryId = repositoryId,
            SnapshotId = snapshotId,
            AnalysisRunId = analysisRunId,
            JobId = jobId,
            Score = score,
            TotalIssues = totalIssues,
            CriticalCount = criticalCount,
            WarningCount = warningCount,
            InfoCount = infoCount,
            SummaryJson = summaryJson,
            CreatedAt = now
        };
    }
}
