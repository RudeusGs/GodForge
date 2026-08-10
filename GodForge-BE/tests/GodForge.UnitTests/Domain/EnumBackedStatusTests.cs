using GodForge.Domain.Entities.Admin;
using GodForge.Domain.Entities.Analysis;
using GodForge.Domain.Entities.Collab;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;

namespace GodForge.UnitTests.Domain;

public sealed class EnumBackedStatusTests
{
    private readonly DateTimeOffset _now = new(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EveryDomainEntityStatusProperty_IsAnEnum()
    {
        var invalidProperties = typeof(AiAnalysisRun).Assembly
            .GetTypes()
            .Where(type => type.Namespace?.StartsWith("GodForge.Domain.Entities", StringComparison.Ordinal) == true)
            .Select(type => new { Type = type, Property = type.GetProperty("Status") })
            .Where(item => item.Property is not null && !item.Property.PropertyType.IsEnum)
            .Select(item => $"{item.Type.FullName}.Status")
            .ToArray();

        Assert.Empty(invalidProperties);
    }

    [Fact]
    public void RunEntities_UseTypedLifecycleStates()
    {
        var run = AiAnalysisRun.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new string('a', 40),
            "default",
            "provider",
            "model",
            "prompt-v1",
            "input-hash",
            _now);

        Assert.Equal(RunStatus.Running, run.Status);

        run.MarkCompleted("summary", null, null, null, null, _now.AddMinutes(1));

        Assert.Equal(RunStatus.Completed, run.Status);
    }

    [Fact]
    public void ProcessingEntities_UseTypedLifecycleStates()
    {
        var snapshot = RepositorySnapshot.Create(
            Guid.NewGuid(),
            new string('b', 40),
            "main",
            _now);

        Assert.Equal(ProcessingStatus.Processing, snapshot.Status);

        snapshot.MarkAsReady(null);

        Assert.Equal(ProcessingStatus.Ready, snapshot.Status);
    }

    [Fact]
    public void CollaborationEntities_UseTypedStates()
    {
        var comment = Comment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "finding",
            Guid.NewGuid().ToString("N"),
            "content",
            null,
            _now);
        var thread = ReviewThread.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid().ToString("N"),
            Guid.NewGuid(),
            _now);

        Assert.Equal(CommentStatus.Active, comment.Status);
        Assert.Equal(ReviewThreadStatus.Open, thread.Status);

        comment.SoftDelete(_now.AddMinutes(1));
        thread.Resolve(_now.AddMinutes(1));

        Assert.Equal(CommentStatus.Deleted, comment.Status);
        Assert.Equal(ReviewThreadStatus.Resolved, thread.Status);
    }

    [Fact]
    public void BoundaryFactories_RequireStatusEnums()
    {
        var activity = Activity.Create(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            "project.updated",
            "project",
            Guid.NewGuid().ToString("N"),
            ActivityStatus.Succeeded,
            null,
            "correlation-id",
            _now);
        var login = LoginEvent.Create(
            Guid.NewGuid(),
            "127.0.0.1",
            "browser",
            "test-agent",
            LoginEventStatus.Succeeded,
            null,
            _now);
        var health = SystemHealthCheck.Create(
            "database",
            SystemHealthStatus.Healthy,
            null,
            _now);
        var heartbeat = WorkerHeartbeat.Create(
            "repository-worker",
            "worker-1",
            ["repository-analysis"],
            null,
            _now);

        heartbeat.Heartbeat(WorkerHeartbeatStatus.Running, _now.AddSeconds(10));

        Assert.Equal(ActivityStatus.Succeeded, activity.Status);
        Assert.Equal(LoginEventStatus.Succeeded, login.Status);
        Assert.Equal(SystemHealthStatus.Healthy, health.Status);
        Assert.Equal(WorkerHeartbeatStatus.Running, heartbeat.Status);
    }
}
