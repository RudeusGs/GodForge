using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Enums;

namespace GodForge.UnitTests.Domain.Ops;

public sealed class JobTests
{
    private readonly DateTimeOffset _now = new(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MarkRunning_CannotRestartCompletedJob()
    {
        var job = CreateJob();
        job.MarkRunning(_now);
        job.MarkCompleted("done", _now.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => job.MarkRunning(_now.AddMinutes(2)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public void UpdateProgress_RejectsValuesOutsideRunningRange(int progress)
    {
        var job = CreateJob();
        job.MarkRunning(_now);

        Assert.Throws<ArgumentOutOfRangeException>(() => job.UpdateProgress(progress, _now));
    }

    [Fact]
    public void UpdateProgress_CannotMoveBackwards()
    {
        var job = CreateJob();
        job.MarkRunning(_now);
        job.UpdateProgress(50, _now.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => job.UpdateProgress(40, _now.AddMinutes(2)));
    }

    [Fact]
    public void MarkRetrying_RequiresFutureAvailability()
    {
        var job = CreateJob();
        job.MarkRunning(_now);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            job.MarkRetrying("TRANSIENT", "failed", _now, _now));
    }


    [Fact]
    public void ClaimToken_ChangesOnRunAndClearsWhenExecutionEnds()
    {
        var job = CreateJob();

        job.MarkRunning(_now);
        var claimToken = job.ClaimToken;

        Assert.NotNull(claimToken);
        job.MarkRetrying("TRANSIENT", "failed", _now.AddMinutes(1), _now);
        Assert.Null(job.ClaimToken);

        job.MarkRunning(_now.AddMinutes(1));
        Assert.NotNull(job.ClaimToken);
        Assert.NotEqual(claimToken, job.ClaimToken);
        job.MarkCompleted("done", _now.AddMinutes(2));
        Assert.Null(job.ClaimToken);
    }

    private Job CreateJob()
        => Job.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            JobType.AnalyzeProject,
            "queue",
            0,
            "{}",
            "key",
            3,
            Guid.NewGuid(),
            "corr",
            _now,
            _now);
}
