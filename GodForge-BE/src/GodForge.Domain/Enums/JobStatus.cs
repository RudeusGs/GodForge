namespace GodForge.Domain.Enums;

public enum JobStatus
{
    Queued,
    Running,
    Retrying,
    Completed,
    Failed,
    Cancelled,
    Timeout,
    DeadLettered
}
