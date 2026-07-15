using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Features.Analysis.Queries.GetAiAdvisory;
using GodForge.Domain.Entities.Analysis;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using Moq;
using Xunit;

namespace GodForge.UnitTests.Application.Features.Analysis.Queries.GetAiAdvisory;

public sealed class GetAiAdvisoryQueryHandlerTests
{
    private readonly Mock<IProjectRepository> _projectsMock = new();
    private readonly Mock<IAiAnalysisRepository> _aiRepositoryMock = new();
    private readonly GetAiAdvisoryQueryHandler _handler;

    public GetAiAdvisoryQueryHandlerTests()
    {
        _handler = new GetAiAdvisoryQueryHandler(_projectsMock.Object, _aiRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _projectsMock.Setup(p => p.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var query = new GetAiAdvisoryQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("PROJECT_NOT_FOUND", result.Error?.Code);
    }

    [Fact]
    public async Task Handle_NoAdvisoryFound_ReturnsNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = Project.Create("Test Project", "test-project", "Desc", "4.2.1", ProjectVisibility.Private, Guid.NewGuid(), DateTimeOffset.UtcNow);
        _projectsMock.Setup(p => p.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _aiRepositoryMock.Setup(a => a.GetLatestByProjectAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiAnalysisRun?)null);

        var query = new GetAiAdvisoryQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ADVISORY_NOT_FOUND", result.Error?.Code);
    }

    [Fact]
    public async Task Handle_Success_ReturnsAdvisoryWithFindings()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        var project = Project.Create("Test Project", "test-project", "Desc", "4.2.1", ProjectVisibility.Private, Guid.NewGuid(), DateTimeOffset.UtcNow);
        _projectsMock.Setup(p => p.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var run = AiAnalysisRun.Create(
            projectId,
            repositoryId,
            "abc1234",
            "health_overview",
            "gemini",
            "gemini-1.5-pro",
            "health-overview-v1",
            "hash123",
            DateTimeOffset.UtcNow.AddMinutes(-5));
        
        run.MarkCompleted("Great repo", 1000, 500, null, null, DateTimeOffset.UtcNow);

        _aiRepositoryMock.Setup(a => a.GetLatestByProjectAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var finding = AiFinding.Create(
            run.Id,
            "general",
            "warning",
            "Missing script",
            "Script not found",
            "Add the script",
            0.9m,
            "[]",
            "fingerprint123",
            DateTimeOffset.UtcNow);

        _aiRepositoryMock.Setup(a => a.GetFindingsByRunAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiFinding> { finding });

        var query = new GetAiAdvisoryQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(run.Id, result.Value.Advisory.Id);
        Assert.Equal("gemini", result.Value.Advisory.Provider);
        Assert.Equal("completed", result.Value.Advisory.Status);
        
        Assert.Single(result.Value.Findings);
        Assert.Equal("warning", result.Value.Findings[0].Severity);
        Assert.Equal("Missing script", result.Value.Findings[0].Title);
    }
}
