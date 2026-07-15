using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models.Messages;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Repositories.Commands.TriggerRepositoryAnalysis;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using Moq;

namespace GodForge.UnitTests.Application.Repositories.Commands;

public sealed class TriggerRepositoryAnalysisCommandHandlerTests
{
    [Fact]
    public async Task Handle_TwoManualRequests_CreateDistinctJobsSoRemoteHeadCanAdvance()
    {
        var projectId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var repository = GitRepository.CreateLinked(
            projectId,
            "https://example.com/team/game.git",
            GitProvider.Generic,
            "main",
            null,
            false,
            now);

        var repositories = new Mock<IGitRepositoryRepository>();
        repositories.Setup(x => x.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        var jobs = new Mock<IJobRepository>();
        var createdJobs = new List<Job>();
        jobs.Setup(x => x.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
            .Callback<Job, CancellationToken>((job, _) => createdJobs.Add(job))
            .Returns(Task.CompletedTask);

        var authorization = new Mock<IAuthorizationService>();
        authorization.Setup(x => x.HasPermissionAsync(
                actorId,
                projectId,
                Permissions.AnalysisTrigger,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var publisher = new Mock<IJobPublisher>();
        publisher.Setup(x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<WorkerMessage>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(now);

        var handler = new TriggerRepositoryAnalysisCommandHandler(
            repositories.Object,
            jobs.Object,
            authorization.Object,
            publisher.Object,
            unitOfWork.Object,
            clock.Object);
        var command = new TriggerRepositoryAnalysisCommand(
            projectId,
            "main",
            "health_overview",
            false,
            actorId,
            "correlation-id");

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value, second.Value);
        Assert.Equal(2, createdJobs.Count);
        Assert.NotEqual(createdJobs[0].IdempotencyKey, createdJobs[1].IdempotencyKey);
        publisher.Verify(x => x.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<RepositoryAnalysisJobMessage>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
