using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using Moq;

namespace GodForge.UnitTests.Application.Projects;

public sealed class ProjectManagementServiceTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IProjectMemberRepository> _members = new();
    private readonly Mock<IOrganizationRepository> _organizations = new();
    private readonly Mock<IOrganizationMemberRepository> _organizationMembers = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IIdempotencyRepository> _idempotency = new();
    private readonly Mock<IAuditWriter> _audit = new();
    private readonly Mock<IM1QuotaPolicy> _quota = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task ListAsync_LoadsMembershipsInOneBatch()
    {
        var now = DateTimeOffset.UtcNow;
        var actorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var first = Project.Create(organizationId, "First", "first", null, "4.3", ProjectVisibility.Private, actorId, now);
        var second = Project.Create(organizationId, "Second", "second", null, "4.3", ProjectVisibility.Private, actorId, now);
        var firstMembership = ProjectMember.Create(first.Id, organizationId, actorId, ProjectRole.ProjectOwner, ProjectMemberSource.Direct, actorId, now);
        var secondMembership = ProjectMember.Create(second.Id, organizationId, actorId, ProjectRole.Developer, ProjectMemberSource.Direct, actorId, now);
        _projects.Setup(repository => repository.GetVisibleProjectsAsync(
                actorId, 1, 20, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Project>(new[] { first, second }, 1, 20, 2));
        _members.Setup(repository => repository.GetMembershipsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { firstMembership, secondMembership });

        var result = await CreateService().ListAsync(actorId, 1, 20, null, null, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        _members.Verify(repository => repository.GetMembershipsAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2), actorId, It.IsAny<CancellationToken>()), Times.Once);
        _members.Verify(repository => repository.GetMembershipAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMemberAsync_WithUndefinedNumericRole_ReturnsValidationBeforeTransaction()
    {
        var result = await CreateService().UpdateMemberAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "999",
            1,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.Error?.Code);
        _unitOfWork.Verify(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMemberAsync_WhenDemotingLastOwner_ReturnsConflictInsideResourceLock()
    {
        var now = DateTimeOffset.UtcNow;
        var actorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var project = Project.Create(organizationId, "Project", "project", null, "4.3", ProjectVisibility.Private, actorId, now);
        var projectMembership = ProjectMember.Create(project.Id, organizationId, actorId, ProjectRole.ProjectOwner, ProjectMemberSource.Direct, actorId, now);
        var organizationMembership = OrganizationMember.CreateOwner(organizationId, actorId, now);
        var user = User.Create("owner@example.com", "Owner", "hash", now);

        _projects.Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _organizationMembers.Setup(repository => repository.GetAsync(organizationId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(organizationMembership);
        _members.Setup(repository => repository.GetMembershipAsync(project.Id, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(projectMembership);
        _members.Setup(repository => repository.GetAnyMembershipAsync(project.Id, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(projectMembership);
        _members.Setup(repository => repository.GetOwnerCountAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _users.Setup(repository => repository.GetByIdAsync(actorId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _unitOfWork.Setup(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unit => unit.AcquireResourceLockAsync("project-membership", project.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unit => unit.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateService().UpdateMemberAsync(
            actorId, project.Id, actorId, "viewer", projectMembership.Version, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("LAST_OWNER_REQUIRED", result.Error?.Code);
        _unitOfWork.Verify(unit => unit.AcquireResourceLockAsync(
            "project-membership", project.Id, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unit => unit.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private ProjectManagementService CreateService()
    {
        var lifecycle = new ProjectLifecycleService(
            _projects.Object,
            _members.Object,
            _organizations.Object,
            _organizationMembers.Object,
            _idempotency.Object,
            _audit.Object,
            _quota.Object,
            _clock.Object,
            _unitOfWork.Object);
        var membership = new ProjectMembershipService(
            _projects.Object,
            _members.Object,
            _organizationMembers.Object,
            _users.Object,
            _audit.Object,
            _clock.Object,
            _unitOfWork.Object);
        return new ProjectManagementService(lifecycle, membership);
    }
}
