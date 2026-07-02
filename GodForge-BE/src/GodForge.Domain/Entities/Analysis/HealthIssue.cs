using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Analysis;

public sealed class HealthIssue : BaseEntity
{
    public Guid ReportId { get; private set; }
    public Guid? RuleId { get; private set; }
    public string IssueType { get; private set; } = default!;
    public string Severity { get; private set; } = default!;
    public string? FilePath { get; private set; }
    public string? NodePath { get; private set; }
    public string Message { get; private set; } = default!;
    public string? DetailsJson { get; private set; }
    public bool IsSuppressed { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private HealthIssue() { } // EF Core

    public static HealthIssue Create(
        Guid reportId, Guid? ruleId, string issueType,
        string severity, string? filePath, string? nodePath,
        string message, string? detailsJson, bool isSuppressed, DateTimeOffset now)
    {
        return new HealthIssue
        {
            Id = Guid.NewGuid(),
            ReportId = reportId,
            RuleId = ruleId,
            IssueType = issueType,
            Severity = severity,
            FilePath = filePath,
            NodePath = nodePath,
            Message = message,
            DetailsJson = detailsJson,
            IsSuppressed = isSuppressed,
            CreatedAt = now
        };
    }
}
