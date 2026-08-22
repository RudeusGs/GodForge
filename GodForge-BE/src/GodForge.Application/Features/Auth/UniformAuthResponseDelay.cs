using System.Diagnostics;

namespace GodForge.Application.Features.Auth;

internal static class UniformAuthResponseDelay
{
    private static readonly TimeSpan MinimumDuration = TimeSpan.FromMilliseconds(150);

    public static long Start() => Stopwatch.GetTimestamp();

    public static async Task CompleteAsync(long startedAt, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var remaining = MinimumDuration - elapsed;
        if (remaining > TimeSpan.Zero)
            await Task.Delay(remaining, cancellationToken);
    }
}
