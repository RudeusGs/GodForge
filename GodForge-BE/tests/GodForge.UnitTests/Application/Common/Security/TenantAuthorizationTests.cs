using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Security;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using Moq;

namespace GodForge.UnitTests.Application.Common.Security;

public sealed class TenantAuthorizationTests
{
    private readonly Mock<IProjectMemberRepository> _projectMembers = new();
    private readonly Mock<IOrganizationMemberRepository> _organizationMembers = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly AuthorizationService _sut;

    public TenantAuthorizationTests()
        => _sut = new AuthorizationService(_projectMembers.Object, _organizationMembers.Object, _users.Object, _projects.Object);

    [Theory]
    [InlineData(ProjectRole.ProjectOwner, Permissions.ProjectsUpdate, true)]
    [InlineData(ProjectRole.Maintainer, Permissions.ProjectsUpdate, true)]
    [InlineData(ProjectRole.Developer, Permissions.AnalysisTrigger, true)]
    [InlineData(ProjectRole.Reviewer, Permissions.AnalysisTrigger, false)]
    [InlineData(ProjectRole.Viewer, Permissions.AnalysisRead, true)]
    public async Task HasPermissionAsync_IntersectsActiveOrganizationAndProjectMembership(
        ProjectRole role, string permission, bool expected)
    {
        var now = DateTimeOffset.UtcNow;
        var actorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var project = Project.Create(organizationId, "Project", "project", null, "4.3", ProjectVisibility.Private, actorId, now);
        var membership = ProjectMember.Create(project.Id, organizationId, actorId, role, ProjectMemberSource.Direct, actorId, now);
        _users.Setup(x => x.GetByIdAsync(actorId, It.IsAny<CancellationToken>())).ReturnsAsync(User.Create("actor@example.com", "Actor", "hash", now));
        _projects.Setup(x => x.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _organizationMembers.Setup(x => x.IsActiveAsync(organizationId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _projectMembers.Setup(x => x.GetMembershipAsync(project.Id, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(membership);

        Assert.Equal(expected, await _sut.HasPermissionAsync(actorId, project.Id, permission));
    }

    [Fact]
    public async Task HasPermissionAsync_DeniesCrossTenantMemberEvenWhenProjectRoleExists()
    {
        var now = DateTimeOffset.UtcNow;
        var actorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var project = Project.Create(organizationId, "Project", "project", null, "4.3", ProjectVisibility.Internal, actorId, now);
        var membership = ProjectMember.Create(project.Id, organizationId, actorId, ProjectRole.ProjectOwner, ProjectMemberSource.Direct, actorId, now);
        _users.Setup(x => x.GetByIdAsync(actorId, It.IsAny<CancellationToken>())).ReturnsAsync(User.Create("actor@example.com", "Actor", "hash", now));
        _projects.Setup(x => x.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _organizationMembers.Setup(x => x.IsActiveAsync(organizationId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _projectMembers.Setup(x => x.GetMembershipAsync(project.Id, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(membership);

        Assert.False(await _sut.HasPermissionAsync(actorId, project.Id, Permissions.AnalysisRead));
        _projectMembers.Verify(x => x.GetMembershipAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HasPermissionAsync_DeniesInternalProjectWithoutProjectRole()
    {
        var now = DateTimeOffset.UtcNow;
        var actorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var project = Project.Create(organizationId, "Internal", "internal", null, "4.3", ProjectVisibility.Internal, actorId, now);
        _users.Setup(x => x.GetByIdAsync(actorId, It.IsAny<CancellationToken>())).ReturnsAsync(User.Create("actor@example.com", "Actor", "hash", now));
        _projects.Setup(x => x.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _organizationMembers.Setup(x => x.IsActiveAsync(organizationId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _projectMembers.Setup(x => x.GetMembershipAsync(project.Id, actorId, It.IsAny<CancellationToken>())).ReturnsAsync((ProjectMember?)null);

        Assert.False(await _sut.HasPermissionAsync(actorId, project.Id, Permissions.AnalysisRead));
    }
}
