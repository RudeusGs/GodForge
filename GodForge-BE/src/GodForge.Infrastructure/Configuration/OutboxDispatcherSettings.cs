using System.ComponentModel.DataAnnotations;

namespace GodForge.Infrastructure.Configuration;

public sealed class OutboxDispatcherSettings
{
    public const string SectionName = "OutboxDispatcher";

    [Range(1, 1000)]
    public int MaxAttempts { get; init; } = 10;
}
