using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Security;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using Moq;

using GodForge.Domain.Entities.Identity;

namespace GodForge.UnitTests.Application.Common.Security;

public class RemovedMemberAuthorizationTests
{
    private readonly Mock<IProjectMemberRepository> _memberRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly AuthorizationService _sut;

    public RemovedMemberAuthorizationTests()
    {
        _memberRepoMock = new Mock<IProjectMemberRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _sut = new AuthorizationService(_memberRepoMock.Object, _userRepoMock.Object);
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
}
