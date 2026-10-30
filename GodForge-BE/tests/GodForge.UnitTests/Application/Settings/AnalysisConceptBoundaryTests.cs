using GodForge.Application.Common.Models.Analysis;
using GodForge.Application.Features.Repositories.Commands.TriggerRepositoryAnalysis;
using GodForge.Application.Features.Settings;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;

namespace GodForge.UnitTests.Application.Settings;

public sealed class AnalysisConceptBoundaryTests
{
    [Theory]
    [InlineData(AiAdvisoryModes.HealthOverview)]
    [InlineData(AiAdvisoryModes.CommitReview)]
    [InlineData(AiAdvisoryModes.ArchitectureReview)]
    public void AdvisoryModes_AreAcceptedByTriggerButNeverByDeterministicRegistry(string mode)
    {
        var validator = new TriggerRepositoryAnalysisCommandValidator();
        Assert.True(validator.Validate(new TriggerRepositoryAnalysisCommand(Guid.NewGuid(), null, mode, false, Guid.NewGuid(), "test")).IsValid);
        var project = Project.Create(Guid.NewGuid(), "Game", "game", null, "unknown", ProjectVisibility.Private, Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.False(AnalysisProfileRegistry.CanSelect(project, mode));
    }

    [Theory]
    [InlineData(AnalysisProfileRegistry.DefaultKey)]
    [InlineData("HEALTH_OVERVIEW")]
    [InlineData("unknown")]
    public void Trigger_RejectsDeterministicKeysAndUnsupportedAdvisoryModes(string key)
    {
        var result = new TriggerRepositoryAnalysisCommandValidator().Validate(
            new TriggerRepositoryAnalysisCommand(Guid.NewGuid(), null, key, true, Guid.NewGuid(), "test"));
        Assert.Contains(result.Errors, error => error.PropertyName == "AnalysisProfile");
    }
}
