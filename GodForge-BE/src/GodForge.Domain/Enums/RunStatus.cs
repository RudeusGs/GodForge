namespace GodForge.Domain.Enums;

/// <summary>
/// Lifecycle shared by bounded execution records that start, then complete or fail.
/// </summary>
public enum RunStatus
{
    Running,
    Completed,
    Failed
}
