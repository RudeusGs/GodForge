using GodForge.Application.Common.Text;

namespace GodForge.UnitTests.Application.Common.Text;

public sealed class SlugGeneratorTests
{
    [Theory]
    [InlineData("New Project", "new-project")]
    [InlineData("Đồ án Godot", "do-an-godot")]
    [InlineData("  Multiple---spaces  ", "multiple-spaces")]
    public void Generate_ReturnsStableSafeSlug(string input, string expected)
    {
        Assert.Equal(expected, SlugGenerator.Generate(input));
    }

    [Fact]
    public void Generate_ReturnsNonEmptyFallback_WhenNameHasNoAsciiLettersOrDigits()
    {
        var slug = SlugGenerator.Generate("你好");

        Assert.StartsWith("project-", slug, StringComparison.Ordinal);
        Assert.True(slug.Length <= 80);
    }
}
