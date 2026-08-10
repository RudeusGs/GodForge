using GodForge.Application.Common.Models.Analysis;
using GodForge.Application.Common.Text;
using GodForge.Domain.Enums;

namespace GodForge.UnitTests.Application.Common.Text;

public sealed class EnumTextTests
{
    [Fact]
    public void ToCamelCase_PreservesExistingApiStatusContract()
    {
        Assert.Equal("completed", EnumText.ToCamelCase(RunStatus.Completed));
        Assert.Equal("inProgress", EnumText.ToCamelCase(FindingStatus.InProgress));
    }

    [Fact]
    public void ToSnakeCase_PreservesWorkerResultStatusContract()
    {
        Assert.Equal("not_requested", EnumText.ToSnakeCase(AiStageStatus.NotRequested));
        Assert.Equal("completed", EnumText.ToSnakeCase(AiStageStatus.Completed));
    }

    [Theory]
    [InlineData("private", ProjectVisibility.Private)]
    [InlineData("INTERNAL", ProjectVisibility.Internal)]
    public void TryParseDefined_AcceptsNamedValues(string value, ProjectVisibility expected)
    {
        Assert.True(EnumText.TryParseDefined<ProjectVisibility>(value, out var parsed));
        Assert.Equal(expected, parsed);
    }

    [Theory]
    [InlineData("999")]
    [InlineData("-1")]
    [InlineData("")]
    public void TryParseDefined_RejectsUndefinedNumericAndEmptyValues(string value)
    {
        Assert.False(EnumText.TryParseDefined<ProjectVisibility>(value, out _));
    }
}
