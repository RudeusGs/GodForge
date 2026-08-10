using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models.Messages;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using GodForge.Worker.Handlers;
using GodForge.Worker.Handlers.Stages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace GodForge.UnitTests.Worker.Handlers;

public sealed class RepositoryAnalysisPipelineHandlerTests
{
    private readonly Mock<IJobRepository> _jobs = new();
    private readonly Mock<IGitRepositoryRepository> _repositories = new();
    private readonly Mock<IOutboxWriter> _outbox = new();
    private readonly Mock<IRepositorySnapshotRepository> _snapshots = new();
    private readonly Mock<IAiAnalysisRepository> _aiRepository = new();
    private readonly Mock<IHealthReportRepository> _healthReports = new();
    private readonly Mock<IDependencyGraphSnapshotRepository> _graphs = new();
    private readonly Mock<IAnalysisRunRepository> _runs = new();
    private readonly Mock<IRepositoryWorkspaceService> _workspaceService = new();
    private readonly Mock<IDeterministicProjectAnalyzer> _deterministicAnalyzer = new();
    private readonly Mock<IDependencyGraphBuilder> _graphBuilder = new();
    private readonly Mock<IRepositoryContextBuilder> _contextBuilder = new();
    private readonly Mock<IAiAnalysisProvider> _aiProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<ILogger<RepositoryAnalysisPipelineHandler>> _logger = new();
    private readonly RepositoryAnalysisPipelineHandler _handler;
    private readonly DateTimeOffset _now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

    public RepositoryAnalysisPipelineHandlerTests()
    {
        _clock.Setup(clock => clock.UtcNow).Returns(_now);
        _outbox.Setup(value => value.EnqueueScheduledAsync(
                It.IsAny<string>(),
                It.IsAny<WorkerMessage>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var deterministicStage = new RepositoryDeterministicAnalysisStage(
            _workspaceService.Object,
            _deterministicAnalyzer.Object,
            _contextBuilder.Object);
        var persistenceStage = new RepositoryAnalysisPersistenceStage(
            _snapshots.Object,
            _healthReports.Object,
            _graphs.Object,
            _runs.Object,
            _graphBuilder.Object,
            _clock.Object);
        var aiStage = new RepositoryAiAnalysisStage(
            _aiRepository.Object,
            _aiProvider.Object,
            _clock.Object);

        _handler = new RepositoryAnalysisPipelineHandler(
            _jobs.Object,
            _repositories.Object,
            _outbox.Object,
            deterministicStage,
            persistenceStage,
            aiStage,
            _unitOfWork.Object,
            Mock.Of<IServiceScopeFactory>(),
            _clock.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenJobDoesNotExist_ReturnsDeadLetter()
    {
        var message = new RepositoryAnalysisJobMessage { JobId = Guid.NewGuid() };
        _jobs.Setup(repository => repository.TryClaimAsync(
                message.JobId,
                _now,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);
        _jobs.Setup(repository => repository.GetByIdAsync(message.JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var result = await _handler.HandleAsync(message, CancellationToken.None);

        Assert.Equal(JobExecutionDisposition.DeadLetter, result.Disposition);
    }

    [Fact]
    public async Task HandleAsync_WhenJobIsAlreadyCompleted_ReturnsCompleted()
    {
        var message = new RepositoryAnalysisJobMessage { JobId = Guid.NewGuid() };
        var job = CreateJob();
        job.MarkRunning(_now);
        job.MarkCompleted("success", _now);
        _jobs.Setup(repository => repository.TryClaimAsync(
                message.JobId,
                _now,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);
        _jobs.Setup(repository => repository.GetByIdAsync(message.JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.HandleAsync(message, CancellationToken.None);

        Assert.Equal(JobExecutionDisposition.Completed, result.Disposition);
    }

    [Fact]
    public async Task HandleAsync_WhenAnotherWorkerOwnsFreshClaim_SchedulesRecoveryAndAcknowledgesDuplicate()
    {
        var job = CreateJob();
        var message = new RepositoryAnalysisJobMessage { JobId = job.Id };
        job.MarkRunning(_now);
        _jobs.Setup(repository => repository.TryClaimAsync(
                message.JobId,
                _now,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);
        _jobs.Setup(repository => repository.GetByIdAsync(message.JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.HandleAsync(message, CancellationToken.None);

        Assert.Equal(JobExecutionDisposition.Completed, result.Disposition);
        _outbox.Verify(value => value.EnqueueScheduledAsync(
            job.QueueName,
            It.Is<RepositoryAnalysisJobMessage>(retry =>
                retry.JobId == job.Id &&
                retry.MessageId != message.MessageId &&
                retry.AttemptCount == job.AttemptCount),
            _now.AddMinutes(30),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repositories.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryDoesNotExist_MarksClaimedJobDeadLettered()
    {
        var repositoryId = Guid.NewGuid();
        var message = new RepositoryAnalysisJobMessage
        {
            JobId = Guid.NewGuid(),
            RepositoryId = repositoryId,
            ProjectId = Guid.NewGuid()
        };
        var job = CreateJob(repositoryId);
        job.MarkRunning(_now);
        _jobs.Setup(repository => repository.TryClaimAsync(
                message.JobId,
                _now,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobs.Setup(repository => repository.IsCancellationRequestedAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositories.Setup(repository => repository.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitRepository?)null);

        var result = await _handler.HandleAsync(message, CancellationToken.None);

        Assert.Equal(JobExecutionDisposition.DeadLetter, result.Disposition);
        Assert.Equal(JobStatus.DeadLettered, job.Status);
        Assert.Equal("REPOSITORY_NOT_CONNECTED", job.ErrorCode);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task HandleAsync_WhenTransientExecutionFails_PersistsScheduledRetryInOutbox()
    {
        var projectId = Guid.NewGuid();
        var repository = GitRepository.Create(
            projectId,
            "https://example.com/repository.git",
            GitProvider.Generic,
            "main",
            _now);
        var job = CreateJob(repository.Id, projectId);
        job.MarkRunning(_now);
        var message = new RepositoryAnalysisJobMessage
        {
            JobId = job.Id,
            ProjectId = projectId,
            RepositoryId = repository.Id,
            CorrelationId = "corr-1",
            Branch = "main"
        };

        _jobs.Setup(value => value.TryClaimAsync(
                message.JobId,
                _now,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobs.Setup(value => value.GetByIdAsync(message.JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobs.Setup(value => value.IsCancellationRequestedAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositories.Setup(value => value.GetByIdAsync(repository.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);
        _workspaceService.Setup(value => value.SyncAsync(
                repository.Id,
                repository.RemoteUrl,
                message.Branch,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("transient git failure"));

        var result = await _handler.HandleAsync(message, CancellationToken.None);

        Assert.Equal(JobExecutionDisposition.Completed, result.Disposition);
        Assert.Equal(JobStatus.Retrying, job.Status);
        _outbox.Verify(value => value.EnqueueScheduledAsync(
            job.QueueName,
            It.Is<RepositoryAnalysisJobMessage>(retry =>
                retry.JobId == job.Id &&
                retry.MessageId != message.MessageId &&
                retry.AttemptCount == job.AttemptCount),
            job.AvailableAt,
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task HandleAsync_WhenRetryIsAlreadyScheduled_AcknowledgesDuplicateDelivery()
    {
        var message = new RepositoryAnalysisJobMessage { JobId = Guid.NewGuid() };
        var job = CreateJob();
        job.MarkRunning(_now);
        job.MarkRetrying("TRANSIENT", "retry", _now.AddMinutes(1), _now);
        _jobs.Setup(value => value.TryClaimAsync(
                message.JobId,
                _now,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);
        _jobs.Setup(value => value.GetByIdAsync(message.JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.HandleAsync(message, CancellationToken.None);

        Assert.Equal(JobExecutionDisposition.Completed, result.Disposition);
        _outbox.VerifyNoOtherCalls();
    }

    private Job CreateJob(Guid? repositoryId = null, Guid? projectId = null)
        => Job.Create(
            projectId ?? Guid.NewGuid(),
            repositoryId,
            JobType.AnalyzeProject,
            "queue",
            0,
            "{}",
            "key",
            3,
            Guid.NewGuid(),
            "corr-1",
            _now,
            _now);
}
