using GodForge.Application.Common.Interfaces;

namespace GodForge.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
