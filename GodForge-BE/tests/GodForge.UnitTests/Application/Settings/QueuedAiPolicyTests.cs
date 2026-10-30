using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Application.Common.Models.Messages;
using GodForge.Application.Features.Settings;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using GodForge.Worker.Handlers.Stages;
using Moq;

namespace GodForge.UnitTests.Application.Settings;

public sealed class QueuedAiPolicyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stage_EnforcesPolicyBeforeProviderAndDiscardsRevokedInFlightOutput(bool initiallyAllowed)
    {
        var policy = new Mock<IAiExecutionPolicy>();
        policy.SetupSequence(p => p.IsAllowedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(initiallyAllowed).ReturnsAsync(initiallyAllowed).ReturnsAsync(false);
        var provider = new Mock<IAiAnalysisProvider>();
        provider.Setup(p => p.AnalyzeAsync(It.IsAny<AiAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiAnalysisResult(true, true, "discarded", [], 1, 1, null));
        var repository = new Mock<IAiAnalysisRepository>();
        var stage = new RepositoryAiAnalysisStage(repository.Object, provider.Object, Mock.Of<IClock>(), policy.Object);
        var git = GitRepository.CreateLinked(Guid.NewGuid(), "https://example.test/team/game.git", GitProvider.Generic, "main", null, false, DateTimeOffset.UtcNow);
        var result = await stage.StageAsync(new RepositoryAnalysisJobMessage { ProjectId = git.ProjectId, IncludeAi = true }, git,
            new RepositoryDeterministicStageResult(new WorkspaceSyncResult("unused", "abc", "main", 0),
                new DeterministicProjectSummary(true, 0, 0, 0, 0, 0, 0, []), new RepositoryContextArtifact("", "hash", 0, 0, false, [], [])), CancellationToken.None);
        Assert.Equal(AiStageStatus.Disabled, result.Status);
        provider.Verify(p => p.AnalyzeAsync(It.IsAny<AiAnalysisRequest>(), It.IsAny<CancellationToken>()), initiallyAllowed ? Times.Once() : Times.Never());
        repository.Verify(p => p.AddRunAsync(It.IsAny<GodForge.Domain.Entities.Analysis.AiAnalysisRun>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stage_PolicyRevokedDuringCacheLookup_DiscardsCacheAndNeverCallsProvider(bool cached)
    {
        var policy = new Mock<IAiExecutionPolicy>();
        policy.SetupSequence(p => p.IsAllowedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);
        var provider = new Mock<IAiAnalysisProvider>();
        var repository = new Mock<IAiAnalysisRepository>();
        var git = GitRepository.CreateLinked(Guid.NewGuid(), "https://example.test/team/game.git", GitProvider.Generic, "main", null, false, DateTimeOffset.UtcNow);
        var run = GodForge.Domain.Entities.Analysis.AiAnalysisRun.Create(git.ProjectId, git.Id, "abc", "health_overview", "test", "test", "v1", "hash", DateTimeOffset.UtcNow);
        run.MarkCompleted("must not publish", 1, 1, null, null, DateTimeOffset.UtcNow);
        repository.Setup(r => r.GetCompletedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(cached ? run : null);
        var result = await new RepositoryAiAnalysisStage(repository.Object, provider.Object, Mock.Of<IClock>(), policy.Object).StageAsync(
            new RepositoryAnalysisJobMessage { ProjectId = git.ProjectId, IncludeAi = true }, git,
            new RepositoryDeterministicStageResult(new WorkspaceSyncResult("unused", "abc", "main", 0),
                new DeterministicProjectSummary(true, 0, 0, 0, 0, 0, 0, []), new RepositoryContextArtifact("", "hash", 0, 0, false, [], [])), CancellationToken.None);
        Assert.Equal(AiStageStatus.Disabled, result.Status);
        Assert.Null(result.Summary);
        provider.Verify(p => p.AnalyzeAsync(It.IsAny<AiAnalysisRequest>(), It.IsAny<CancellationToken>()), Times.Never());
        repository.Verify(p => p.AddRunAsync(It.IsAny<GodForge.Domain.Entities.Analysis.AiAnalysisRun>(), It.IsAny<CancellationToken>()), Times.Never());
    }
}
