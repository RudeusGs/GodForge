namespace GodForge.Domain.Enums;

/// <summary>
/// Collaboration state of an analysis finding.
/// </summary>
public enum FindingStatus
{
    Open,
    InProgress,
    Resolved,
    Ignored,
    FalsePositive,
    Reopened
}
