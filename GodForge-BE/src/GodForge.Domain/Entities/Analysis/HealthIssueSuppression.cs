using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Analysis;

public sealed class HealthIssueSuppression : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public string RuleCode { get; private set; } = default!;
    public string? FilePath { get; private set; }
    public string? NodePath { get; private set; }
    public string Reason { get; private set; } = default!;
    public Guid SuppressedBy { get; private set; }
    public DateTimeOffset? SuppressedUntil { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private HealthIssueSuppression() { } // EF Core

    public static HealthIssueSuppression Create(
        Guid projectId, string ruleCode, string? filePath,
        string? nodePath, string reason, Guid suppressedBy,
        DateTimeOffset? suppressedUntil, DateTimeOffset now)
    {
        return new HealthIssueSuppression
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RuleCode = ruleCode,
            FilePath = filePath,
            NodePath = nodePath,
            Reason = reason,
            SuppressedBy = suppressedBy,
            SuppressedUntil = suppressedUntil,
            CreatedAt = now
        };
    }
}
