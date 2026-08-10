using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using Xunit;

namespace GodForge.UnitTests.Domain;

public class ProjectEntityTests
{
    [Fact]
    public void Create_ValidState_ShouldInitializeCorrectly()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var project = Project.Create(organizationId, "Test Project", "test-project", "desc", "4.3", ProjectVisibility.Private, creatorId, now);

        // Assert
        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal("Test Project", project.Name);
        Assert.Equal("test-project", project.Slug);
        Assert.Equal("desc", project.Description);
        Assert.Equal(ProjectVisibility.Private, project.Visibility);
        Assert.Equal(ProjectStatus.Active, project.Status);
        Assert.Equal(organizationId, project.OrganizationId);
        Assert.Equal(1, project.Version);
        Assert.Equal(creatorId, project.CreatedBy);
        Assert.Equal(now, project.CreatedAt);
        Assert.Null(project.DeletedAt);
    }

    [Fact]
    public void SoftDelete_ValidState_ShouldSetDeletedAtAndStatus()
    {
        // Arrange
        var project = Project.Create(Guid.NewGuid(), "Test Project", "test-project", "desc", "4.3", ProjectVisibility.Private, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var deleteTime = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        project.SoftDelete(deleteTime);

        // Assert
        Assert.Equal(deleteTime, project.DeletedAt);
        Assert.Equal(ProjectStatus.Deleted, project.Status);
    }

    [Fact]
    public void Create_SlugExceedsDomainLimit_ThrowsArgumentException()
    {
        var slug = new string('a', Project.MaxSlugLength + 1);

        Assert.Throws<ArgumentException>(() => Project.Create(
            Guid.NewGuid(),
            "Test Project",
            slug,
            null,
            "4.3",
            ProjectVisibility.Private,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));
    }
}
