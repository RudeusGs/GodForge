using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models.Messages;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using GodForge.Worker.Handlers;
using Microsoft.Extensions.Logging;
using Moq;

namespace GodForge.UnitTests.Worker.Handlers;

public class RepositoryAnalysisPipelineHandlerTests
{
    private readonly Mock<IJobRepository> _jobsMock;
    private readonly Mock<IGitRepositoryRepository> _repositoriesMock = new();
    private readonly Mock<IRepositorySnapshotRepository> _snapshotsMock = new();
    private readonly Mock<IAiAnalysisRepository> _aiRepositoryMock = new();
    private readonly Mock<IHealthReportRepository> _healthReportsMock = new();
    private readonly Mock<IDependencyGraphSnapshotRepository> _graphsMock = new();
    private readonly Mock<IAnalysisRunRepository> _runsMock = new();
    private readonly Mock<IRepositoryWorkspaceService> _workspaceServiceMock = new();
    private readonly Mock<IDeterministicProjectAnalyzer> _deterministicAnalyzerMock = new();
    private readonly Mock<IDependencyGraphBuilder> _graphBuilderMock = new();
    private readonly Mock<IRepositoryContextBuilder> _contextBuilderMock = new();
    private readonly Mock<IAiAnalysisProvider> _aiProviderMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IClock> _clockMock = new();
    private readonly Mock<ILogger<RepositoryAnalysisPipelineHandler>> _loggerMock = new();
    private readonly RepositoryAnalysisPipelineHandler _handler;
    private readonly DateTimeOffset _now;

    public RepositoryAnalysisPipelineHandlerTests()
    {
        _jobsMock = new Mock<IJobRepository>();
        _repositoriesMock = new Mock<IGitRepositoryRepository>();
        _snapshotsMock = new Mock<IRepositorySnapshotRepository>();
        _aiRepositoryMock = new Mock<IAiAnalysisRepository>();
        _workspaceServiceMock = new Mock<IRepositoryWorkspaceService>();
        _deterministicAnalyzerMock = new Mock<IDeterministicProjectAnalyzer>();
        _contextBuilderMock = new Mock<IRepositoryContextBuilder>();
        _aiProviderMock = new Mock<IAiAnalysisProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _clockMock = new Mock<IClock>();
        _loggerMock = new Mock<ILogger<RepositoryAnalysisPipelineHandler>>();

        _now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        _clockMock.Setup(c => c.UtcNow).Returns(_now);

        _graphBuilderMock.Setup(b => b.BuildAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GodForge.Domain.Entities.Analysis.DependencyGraphSnapshot.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "hash", 0, 0, 0, DateTimeOffset.UtcNow), new List<GodForge.Domain.Entities.Analysis.DependencyGraphNode>(), new List<GodForge.Domain.Entities.Analysis.DependencyGraphEdge>()));

        _handler = new RepositoryAnalysisPipelineHandler(
            _jobsMock.Object,
            _repositoriesMock.Object,
            _snapshotsMock.Object,
            _aiRepositoryMock.Object,
            _healthReportsMock.Object,
            _graphsMock.Object,
            _runsMock.Object,
            _workspaceServiceMock.Object,
            _deterministicAnalyzerMock.Object,
            _graphBuilderMock.Object,
            _contextBuilderMock.Object,
            _aiProviderMock.Object,
            _unitOfWorkMock.Object,
            _clockMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenJobDoesNotExist_ReturnsDeadLetter()
    {
        var message = new RepositoryAnalysisJobMessage { JobId = Guid.NewGuid() };
        _jobsMock.Setup(j => j.GetByIdAsync(message.JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var result = await _handler.HandleAsync(message, CancellationToken.None);

        Assert.Equal(JobExecutionDisposition.DeadLetter, result.Disposition);
    }

    [Fact]
    public async Task HandleAsync_WhenJobIsAlreadyCompleted_ReturnsCompleted()
    {
        var message = new RepositoryAnalysisJobMessage { JobId = Guid.NewGuid() };
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), JobType.AnalyzeProject, "queue", 0, "{}", "key", 3, Guid.NewGuid(), "corr-1", _now, _now);
        job.MarkRunning(_now);
        job.MarkCompleted("success", _now);
        _jobsMock.Setup(j => j.GetByIdAsync(message.JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.HandleAsync(message, CancellationToken.None);

        Assert.Equal(JobExecutionDisposition.Completed, result.Disposition);
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryDoesNotExist_MarksJobDeadLettered()
    {
        var message = new RepositoryAnalysisJobMessage { JobId = Guid.NewGuid(), RepositoryId = Guid.NewGuid() };
        var job = Job.Create(Guid.NewGuid(), message.RepositoryId.Value, JobType.AnalyzeProject, "queue", 0, "{}", "key", 3, Guid.NewGuid(), "corr-1", _now, _now);
        _jobsMock.Setup(j => j.GetByIdAsync(message.JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _repositoriesMock.Setup(r => r.GetByIdAsync(message.RepositoryId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitRepository?)null);

        var result = await _handler.HandleAsync(message, CancellationToken.None);

        Assert.Equal(JobExecutionDisposition.DeadLetter, result.Disposition);
        Assert.Equal(JobStatus.DeadLettered, job.Status);
        Assert.Equal("REPOSITORY_NOT_CONNECTED", job.ErrorCode);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
