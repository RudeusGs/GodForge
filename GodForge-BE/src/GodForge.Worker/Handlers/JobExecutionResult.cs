namespace GodForge.Worker.Handlers;

public enum JobExecutionDisposition
{
    Completed,
    Retry,
    DeadLetter
}

public sealed record JobExecutionResult(
    JobExecutionDisposition Disposition,
    TimeSpan RetryDelay)
{
    public static JobExecutionResult Completed() => new(JobExecutionDisposition.Completed, TimeSpan.Zero);
    public static JobExecutionResult Retry(TimeSpan delay) => new(JobExecutionDisposition.Retry, delay);
    public static JobExecutionResult DeadLetter() => new(JobExecutionDisposition.DeadLetter, TimeSpan.Zero);
}
