namespace GodForge.Domain.Enums;

public enum OutboxMessageStatus
{
    Pending,
    Processing,
    Processed,
    Failed,
    DeadLettered
}
