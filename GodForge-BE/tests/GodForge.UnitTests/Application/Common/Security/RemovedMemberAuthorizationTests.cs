using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Security;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using Moq;

namespace GodForge.UnitTests.Application.Common.Security;

public class RemovedMemberAuthorizationTests
{
    private readonly Mock<IProjectMemberRepository> _memberRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly AuthorizationService _sut;

    public RemovedMemberAuthorizationTests()
    {
        _memberRepoMock = new Mock<IProjectMemberRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _projectRepoMock = new Mock<IProjectRepository>();
        _sut = new AuthorizationService(_memberRepoMock.Object, _userRepoMock.Object, _projectRepoMock.Object);
    }

    [Fact]
    public async Task HasPermissionAsync_ReturnsFalse_WhenMemberIsRemoved()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var permission = Permissions.RepositoryRead;

        var user = User.Create("test@example.com", "Test User", "hash", DateTimeOffset.UtcNow);
        // Simulate activation
        var method = typeof(User).GetMethod("Activate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method != null) method.Invoke(user, null);

        _userRepoMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Member repo returns null because RemovedAt != null is filtered at the DB level.
        // Therefore, we simulate that behavior by returning null here.
        _memberRepoMock.Setup(x => x.GetMembershipAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectMember?)null);

        // Act
        var result = await _sut.HasPermissionAsync(userId, projectId, permission);

        // Assert
        Assert.False(result, "Removed members (simulated by repo returning null) should not have permissions.");
    }

    [Fact]
    public async Task HasPermissionAsync_AllowsReadForInternalProjectWithoutMembership()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var user = User.Create("reader@example.com", "Reader", "hash", DateTimeOffset.UtcNow);
        var project = Project.Create(
            "Internal Project",
            "internal-project",
            null,
            "4.3",
            ProjectVisibility.Internal,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        _userRepoMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _memberRepoMock.Setup(x => x.GetMembershipAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectMember?)null);
        _projectRepoMock.Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var result = await _sut.HasPermissionAsync(userId, projectId, Permissions.AnalysisRead);

        Assert.True(result);
    }

    [Fact]
    public async Task HasPermissionAsync_DeniesWriteForInternalProjectWithoutMembership()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var user = User.Create("reader@example.com", "Reader", "hash", DateTimeOffset.UtcNow);

        _userRepoMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _memberRepoMock.Setup(x => x.GetMembershipAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectMember?)null);

        var result = await _sut.HasPermissionAsync(userId, projectId, Permissions.AnalysisTrigger);

        Assert.False(result);
        _projectRepoMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
