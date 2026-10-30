using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Features.Settings;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using Moq;

namespace GodForge.UnitTests.Application.Settings;

public sealed class EffectiveSettingsPolicyTests
{
    [Theory]
    [InlineData(false, false, true, false, "platform")]
    [InlineData(false, true, true, false, "platform")]
    [InlineData(true, false, true, false, "organization")]
    [InlineData(true, true, false, false, "project")]
    [InlineData(true, true, true, true, "project")]
    public async Task Policy_UsesIntersectionAndActualRestrictionSource(bool platform, bool organization, bool requested, bool expected, string source)
    {
        var policies = new Mock<IOrganizationSettingRepository>();
        policies.Setup(p => p.GetAiAllowedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        var resolver = new EffectiveSettingsPolicy(new Capabilities(platform), policies.Object);
        var result = await resolver.ResolveAiAsync(Guid.NewGuid(), requested, CancellationToken.None);
        Assert.Equal(expected, result.Value); Assert.Equal(source, result.SourceScope);
        Assert.Equal(platform && organization, result.Editable);
    }

    [Fact]
    public void ProfileRegistry_ExposesOnlyExistingImmutableAnalyzerMode()
    {
        var project = Project.Create(Guid.NewGuid(), "Game", "game", null, "unknown", ProjectVisibility.Private, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var profile = Assert.Single(AnalysisProfileRegistry.For(project));
        Assert.Equal("current-default-v1", profile.Key); Assert.Equal("1", profile.Version);
        Assert.True(AnalysisProfileRegistry.CanSelect(project, profile.Key));
        Assert.False(AnalysisProfileRegistry.CanSelect(project, "current-default-v2"));
        Assert.False(AnalysisProfileRegistry.CanSelect(project, "health_overview"));
    }
    private sealed record Capabilities(bool AiAdvisoryAllowed) : IPlatformCapabilities;
}
