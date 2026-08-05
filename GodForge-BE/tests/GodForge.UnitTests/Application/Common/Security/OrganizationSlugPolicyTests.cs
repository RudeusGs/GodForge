using GodForge.Application.Common.Security;
using Xunit;

namespace GodForge.UnitTests.Application.Common.Security;

public sealed class OrganizationSlugPolicyTests
{
    [Theory]
    [InlineData("admin")]
    [InlineData("api")]
    [InlineData("organizations")]
    [InlineData("projects")]
    [InlineData("settings")]
    public void IsReserved_KnownPlatformSlug_ReturnsTrue(string slug)
        => Assert.True(OrganizationSlugPolicy.IsReserved(slug));

    [Fact]
    public void IsReserved_NormalOrganizationSlug_ReturnsFalse()
        => Assert.False(OrganizationSlugPolicy.IsReserved("studio-name"));
}
