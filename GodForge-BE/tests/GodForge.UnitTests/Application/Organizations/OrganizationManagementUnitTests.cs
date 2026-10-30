using GodForge.Application.Common.Idempotency;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Organizations.Commands.AcceptOrganizationInvitation;
using GodForge.Application.Features.Organizations.Commands.CreateOrganizationInvitation;
using GodForge.Application.Features.Organizations.Commands.RemoveOrganizationMember;
using GodForge.Application.Features.Organizations.Commands.RequestOrganizationDeletion;
using GodForge.Application.Features.Organizations.Commands.RevokeOrganizationInvitation;
using GodForge.Application.Features.Organizations.Commands.TransferOrganizationOwnership;
using GodForge.Application.Features.Organizations.Commands.UpdateOrganization;
using GodForge.Application.Features.Organizations.Commands.UpdateOrganizationMember;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Enums;
using Moq;

namespace GodForge.UnitTests.Application.Organizations;

public sealed class OrganizationManagementUnitTests
{
    private readonly DateTimeOffset _now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
    private readonly Mock<IOrganizationRepository> _organizations = new();
    private readonly Mock<IOrganizationMemberRepository> _members = new();
    private readonly Mock<IProjectMemberRepository> _projectMembers = new();
    private readonly Mock<IUserInviteRepository> _invitations = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IIdempotencyRepository> _idempotency = new();
    private readonly Mock<ISecretHashService> _secretHash = new();
    private readonly Mock<IFrontendUrlBuilder> _frontendUrls = new();
    private readonly Mock<IEmailOutbox> _emailOutbox = new();
    private readonly Mock<IAuditWriter> _auditWriter = new();
    private readonly Mock<IM1QuotaPolicy> _quotaPolicy = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public OrganizationManagementUnitTests()
    {
        _clock.SetupGet(x => x.UtcNow).Returns(_now);
        _quotaPolicy.SetupGet(x => x.MaxPendingInvitationsPerOrganization).Returns(50);
        _secretHash.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(raw => $"hash:{raw}");
        _frontendUrls.Setup(x => x.BuildOrganizationInvitationUrl(It.IsAny<string>()))
            .Returns<string>(token => $"https://app.godforge.test/organization-invitations/accept?token={Uri.EscapeDataString(token)}");
    }

    [Fact]
    public void OrganizationRolePermissions_MemberCanReadOrganizationAndMembersOnly()
    {
        var memberPermissions = OrganizationRolePermissions.GetPermissionsForRole(OrganizationRole.OrganizationMember);

        Assert.Contains(Permissions.OrganizationsRead, memberPermissions);
        Assert.Contains(Permissions.OrganizationMembersRead, memberPermissions);
        Assert.DoesNotContain(Permissions.OrganizationMembersInvite, memberPermissions);
        Assert.DoesNotContain(Permissions.OrganizationMembersUpdateRole, memberPermissions);
        Assert.DoesNotContain(Permissions.OrganizationMembersRemove, memberPermissions);
        Assert.DoesNotContain(Permissions.OrganizationsUpdate, memberPermissions);
        Assert.DoesNotContain(Permissions.OrganizationsDelete, memberPermissions);
        Assert.DoesNotContain(Permissions.OrganizationsTransferOwnership, memberPermissions);
    }

    [Theory]
    [InlineData(OrganizationRole.OrganizationOwner, OrganizationRole.OrganizationAdmin)]
    [InlineData(OrganizationRole.OrganizationAdmin, OrganizationRole.OrganizationAdmin)]
    [InlineData(OrganizationRole.OrganizationMember, OrganizationRole.OrganizationAdmin)]
    [InlineData(OrganizationRole.OrganizationOwner, OrganizationRole.OrganizationMember)]
    [InlineData(OrganizationRole.OrganizationAdmin, OrganizationRole.OrganizationMember)]
    public async Task UpdateMember_WhenAdminExceedsGrantBoundary_ReturnsForbidden(
        OrganizationRole targetCurrentRole,
        OrganizationRole requestedRole)
    {
        var actorId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var organization = Organization.Create("Studio", "studio", actorId, _now);
        var adminMembership = OrganizationMember.Create(organization.Id, actorId, OrganizationRole.OrganizationAdmin, actorId, _now);
        var targetMembership = OrganizationMember.Create(organization.Id, targetId, targetCurrentRole, actorId, _now);
        var targetUser = User.Create("target@example.com", "Target", "hash", _now);

        _organizations.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        _members.Setup(x => x.GetAsync(organization.Id, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(adminMembership);
        _members.Setup(x => x.GetAsync(organization.Id, targetId, It.IsAny<CancellationToken>())).ReturnsAsync(targetMembership);
        _users.Setup(x => x.GetByIdAsync(targetId, It.IsAny<CancellationToken>())).ReturnsAsync(targetUser);

        var handler = new UpdateOrganizationMemberCommandHandler(
            _organizations.Object,
            _members.Object,
            _projectMembers.Object,
            _unitOfWork.Object,
            _users.Object,
            _auditWriter.Object,
            _clock.Object);

        var result = await handler.Handle(
            new UpdateOrganizationMemberCommand(
                actorId,
                organization.Id,
                targetId,
                requestedRole == OrganizationRole.OrganizationAdmin ? "organizationAdmin" : "organizationMember",
                "active",
                targetMembership.Version),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("SECURITY_FORBIDDEN", result.Error?.Code);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMember_WhenDemotingLastActiveOwner_ReturnsLastOwnerRequired()
    {
        var ownerId = Guid.NewGuid();
        var organization = Organization.Create("Studio", "studio", ownerId, _now);
        var ownerMembership = OrganizationMember.CreateOwner(organization.Id, ownerId, _now);
        var ownerUser = User.Create("owner@example.com", "Owner", "hash", _now);

        _organizations.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        _members.Setup(x => x.GetAsync(organization.Id, ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(ownerMembership);
        _members.Setup(x => x.GetActiveOwnerCountAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _users.Setup(x => x.GetByIdAsync(ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(ownerUser);

        var handler = new UpdateOrganizationMemberCommandHandler(
            _organizations.Object,
            _members.Object,
            _projectMembers.Object,
            _unitOfWork.Object,
            _users.Object,
            _auditWriter.Object,
            _clock.Object);

        var result = await handler.Handle(
            new UpdateOrganizationMemberCommand(ownerId, organization.Id, ownerId, "organizationAdmin", "active", ownerMembership.Version),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("LAST_OWNER_REQUIRED", result.Error?.Code);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveMember_WhenTargetIsSoleProjectOwner_ReturnsLastOwnerRequired()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var organization = Organization.Create("Studio", "studio", ownerId, _now);
        var ownerMembership = OrganizationMember.CreateOwner(organization.Id, ownerId, _now);
        var memberMembership = OrganizationMember.Create(organization.Id, memberId, OrganizationRole.OrganizationMember, ownerId, _now);

        _organizations.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        _members.Setup(x => x.GetAsync(organization.Id, ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(ownerMembership);
        _members.Setup(x => x.GetAsync(organization.Id, memberId, It.IsAny<CancellationToken>())).ReturnsAsync(memberMembership);
        _projectMembers.Setup(x => x.GetActiveByOrganizationUserAsync(organization.Id, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ProjectMember.Create(projectId, organization.Id, memberId, ProjectRole.ProjectOwner, ProjectMemberSource.Direct, ownerId, _now) });
        _projectMembers.Setup(x => x.GetSoleOwnerProjectIdsAsync(organization.Id, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { projectId });

        var handler = new RemoveOrganizationMemberCommandHandler(
            _organizations.Object,
            _members.Object,
            _projectMembers.Object,
            _unitOfWork.Object,
            _auditWriter.Object,
            _clock.Object);

        var result = await handler.Handle(
            new RemoveOrganizationMemberCommand(ownerId, organization.Id, memberId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("LAST_OWNER_REQUIRED", result.Error?.Code);
        _unitOfWork.Verify(x => x.AcquireResourceLockAsync("project-membership", projectId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateInvitation_WhenAdminInvitesAdmin_ReturnsForbidden()
    {
        var adminId = Guid.NewGuid();
        var organization = Organization.Create("Studio", "studio", adminId, _now);
        var adminMembership = OrganizationMember.Create(organization.Id, adminId, OrganizationRole.OrganizationAdmin, adminId, _now);

        _organizations.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        _members.Setup(x => x.GetAsync(organization.Id, adminId, It.IsAny<CancellationToken>())).ReturnsAsync(adminMembership);

        var handler = CreateInvitationHandler();
        var result = await handler.Handle(
            new CreateOrganizationInvitationCommand(adminId, organization.Id, "new-admin@example.com", "organizationAdmin"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("SECURITY_FORBIDDEN", result.Error?.Code);
        _emailOutbox.Verify(x => x.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateInvitation_WithSameIdempotencyKeyAndPayload_ReturnsExistingWithoutDuplicateEmail()
    {
        var ownerId = Guid.NewGuid();
        var organization = Organization.Create("Studio", "studio", ownerId, _now);
        var ownerMembership = OrganizationMember.CreateOwner(organization.Id, ownerId, _now);
        var existingInvitation = UserInvite.Create(
            organization.Id,
            "invitee@example.com",
            OrganizationRole.OrganizationMember,
            "hash:token",
            ownerId,
            _now.AddDays(7),
            _now);
        var requestHash = IdempotencyRequest.Hash($"{organization.Id:N}\nINVITEE@EXAMPLE.COM\n{OrganizationRole.OrganizationMember}");
        var record = IdempotencyRecord.Create(
            ownerId,
            "organization.invitation.create",
            "idem-key-1",
            requestHash,
            "organization-invitation",
            existingInvitation.Id,
            _now);

        _organizations.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        _members.Setup(x => x.GetAsync(organization.Id, ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(ownerMembership);
        _idempotency.Setup(x => x.GetAsync(ownerId, "organization.invitation.create", "idem-key-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _invitations.Setup(x => x.GetByIdAsync(organization.Id, existingInvitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingInvitation);

        var handler = CreateInvitationHandler();
        var result = await handler.Handle(
            new CreateOrganizationInvitationCommand(ownerId, organization.Id, "Invitee@Example.com", "organizationMember", "idem-key-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingInvitation.Id, result.Value!.Id);
        _emailOutbox.Verify(x => x.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcceptInvitation_WhenEmailDoesNotMatchAuthenticatedUser_ReturnsUnauthorized()
    {
        var ownerId = Guid.NewGuid();
        var wrongUser = User.Create("wrong@example.com", "Wrong User", "hash", _now);
        wrongUser.MarkEmailVerified(_now);
        var organization = Organization.Create("Studio", "studio", ownerId, _now);
        var invitation = UserInvite.Create(
            organization.Id,
            "intended@example.com",
            OrganizationRole.OrganizationMember,
            "hash:valid-token",
            ownerId,
            _now.AddDays(7),
            _now);

        _invitations.Setup(x => x.GetByTokenHashAsync("hash:valid-token", It.IsAny<CancellationToken>())).ReturnsAsync(invitation);
        _users.Setup(x => x.GetByIdAsync(wrongUser.Id, It.IsAny<CancellationToken>())).ReturnsAsync(wrongUser);

        var handler = new AcceptOrganizationInvitationCommandHandler(
            _organizations.Object,
            _members.Object,
            _projectMembers.Object,
            _unitOfWork.Object,
            _invitations.Object,
            _users.Object,
            _secretHash.Object,
            _auditWriter.Object,
            _clock.Object);

        var result = await handler.Handle(new AcceptOrganizationInvitationCommand(wrongUser.Id, "valid-token"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVITE_INVALID_OR_EXPIRED", result.Error?.Code);
        Assert.Equal(InviteStatus.Pending, invitation.Status);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeInvitation_WhenAlreadyRevoked_IsIdempotentWithoutDuplicateAudit()
    {
        var ownerId = Guid.NewGuid();
        var organization = Organization.Create("Studio", "studio", ownerId, _now);
        var ownerMembership = OrganizationMember.CreateOwner(organization.Id, ownerId, _now);
        var invitation = UserInvite.Create(
            organization.Id,
            "invitee@example.com",
            OrganizationRole.OrganizationMember,
            "hash:token",
            ownerId,
            _now.AddDays(7),
            _now);
        invitation.Revoke(_now);

        _organizations.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        _members.Setup(x => x.GetAsync(organization.Id, ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(ownerMembership);
        _invitations.Setup(x => x.GetByIdAsync(organization.Id, invitation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invitation);

        var handler = new RevokeOrganizationInvitationCommandHandler(
            _organizations.Object,
            _members.Object,
            _projectMembers.Object,
            _unitOfWork.Object,
            _invitations.Object,
            _auditWriter.Object,
            _clock.Object);

        var result = await handler.Handle(
            new RevokeOrganizationInvitationCommand(ownerId, organization.Id, invitation.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        _auditWriter.Verify(
            x => x.WriteAuditAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RequestOrganizationDeletion_WhenConfirmationSlugMismatches_ReturnsValidationError()
    {
        var ownerId = Guid.NewGuid();
        var organization = Organization.Create("Studio", "studio-slug", ownerId, _now);
        var ownerMembership = OrganizationMember.CreateOwner(organization.Id, ownerId, _now);

        _organizations.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        _members.Setup(x => x.GetAsync(organization.Id, ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(ownerMembership);

        var handler = new RequestOrganizationDeletionCommandHandler(
            _organizations.Object,
            _members.Object,
            _projectMembers.Object,
            _unitOfWork.Object,
            _auditWriter.Object,
            _clock.Object);

        var result = await handler.Handle(
            new RequestOrganizationDeletionCommand(ownerId, organization.Id, 1, "wrong-slug"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.Error?.Code);
        Assert.Equal(OrganizationStatus.Active, organization.Status);
    }

    [Fact]
    public async Task UpdateOrganization_WhenExplicitEmptyNameProvided_ReturnsValidationError()
    {
        var ownerId = Guid.NewGuid();
        var organization = Organization.Create("Studio", "studio-slug", ownerId, _now);
        var ownerMembership = OrganizationMember.CreateOwner(organization.Id, ownerId, _now);

        _organizations.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        _members.Setup(x => x.GetAsync(organization.Id, ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(ownerMembership);

        var handler = new UpdateOrganizationCommandHandler(
            _organizations.Object,
            _members.Object,
            _projectMembers.Object,
            _unitOfWork.Object,
            _auditWriter.Object,
            _clock.Object);

        var result = await handler.Handle(
            new UpdateOrganizationCommand(ownerId, organization.Id, "   ", null, 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.Error?.Code);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("A", "a", true)]
    [InlineData("Valid Studio Name", "valid-studio-123", true)]
    [InlineData("", "valid-slug", false)]
    [InlineData("   ", "valid-slug", false)]
    [InlineData("Valid Name", "", false)]
    [InlineData("Valid Name", "-leading-hyphen", false)]
    [InlineData("Valid Name", "trailing-hyphen-", false)]
    [InlineData("Valid Name", "consecutive--hyphens", false)]
    [InlineData("Valid Name", "Uppercase-Slug", false)]
    [InlineData("Valid Name", "underscore_slug", false)]
    [InlineData("Valid Name", "space slug", false)]
    public void OrganizationValidation_EnforcesNameAndSlugRules(string name, string slug, bool expectedValid)
    {
        var actual = Organization.IsValidName(name) && Organization.IsValidSlug(slug);
        Assert.Equal(expectedValid, actual);
    }

    [Fact]
    public async Task UpdateOrganization_AcceptsExactBoundaryLengths_AndRejectsExceedingLengths()
    {
        var ownerId = Guid.NewGuid();
        var organization = Organization.Create("Studio", "studio-slug", ownerId, _now);
        var ownerMembership = OrganizationMember.CreateOwner(organization.Id, ownerId, _now);

        _organizations.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        _members.Setup(x => x.GetAsync(organization.Id, ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(ownerMembership);

        var handler = new UpdateOrganizationCommandHandler(
            _organizations.Object,
            _members.Object,
            _projectMembers.Object,
            _unitOfWork.Object,
            _auditWriter.Object,
            _clock.Object);

        var maxName = new string('N', Organization.MaxNameLength);
        var maxSlug = new string('s', Organization.MaxSlugLength);

        var validBoundaryResult = await handler.Handle(
            new UpdateOrganizationCommand(ownerId, organization.Id, maxName, maxSlug, 1),
            CancellationToken.None);

        Assert.True(validBoundaryResult.IsSuccess);
        Assert.Equal(maxName, validBoundaryResult.Value!.Name);
        Assert.Equal(maxSlug, validBoundaryResult.Value!.Slug);

        var overMaxNameResult = await handler.Handle(
            new UpdateOrganizationCommand(ownerId, organization.Id, new string('N', Organization.MaxNameLength + 1), maxSlug, 2),
            CancellationToken.None);
        Assert.False(overMaxNameResult.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", overMaxNameResult.Error?.Code);

        var overMaxSlugResult = await handler.Handle(
            new UpdateOrganizationCommand(ownerId, organization.Id, maxName, new string('s', Organization.MaxSlugLength + 1), 2),
            CancellationToken.None);
        Assert.False(overMaxSlugResult.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", overMaxSlugResult.Error?.Code);
    }

    [Fact]
    public async Task RemoveMember_WhenMultipleActiveOwnersExist_AllowsRemovingOneOwner()
    {
        var owner1Id = Guid.NewGuid();
        var owner2Id = Guid.NewGuid();
        var organization = Organization.Create("Studio", "studio", owner1Id, _now);
        var owner1Membership = OrganizationMember.CreateOwner(organization.Id, owner1Id, _now);
        var owner2Membership = OrganizationMember.CreateOwner(organization.Id, owner2Id, _now);

        _organizations.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        _members.Setup(x => x.GetAsync(organization.Id, owner1Id, It.IsAny<CancellationToken>())).ReturnsAsync(owner1Membership);
        _members.Setup(x => x.GetAsync(organization.Id, owner2Id, It.IsAny<CancellationToken>())).ReturnsAsync(owner2Membership);
        _members.Setup(x => x.GetActiveOwnerCountAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _projectMembers.Setup(x => x.GetActiveByOrganizationUserAsync(organization.Id, owner2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProjectMember>());
        _projectMembers.Setup(x => x.GetSoleOwnerProjectIdsAsync(organization.Id, owner2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>());

        var handler = new RemoveOrganizationMemberCommandHandler(
            _organizations.Object,
            _members.Object,
            _projectMembers.Object,
            _unitOfWork.Object,
            _auditWriter.Object,
            _clock.Object);

        var result = await handler.Handle(
            new RemoveOrganizationMemberCommand(owner1Id, organization.Id, owner2Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MembershipStatus.Removed, owner2Membership.Status);
        _unitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransferOwnership_WhenTargetUserAccountIsInactive_ReturnsNotFound()
    {
        var ownerUser = User.Create("owner@example.com", "Owner", "hash", _now);
        var deletedTargetUser = User.Create("target@example.com", "Target", "hash", _now);
        deletedTargetUser.SoftDelete(_now);
        var ownerId = ownerUser.Id;
        var targetId = deletedTargetUser.Id;
        var organization = Organization.Create("Studio", "studio", ownerId, _now);
        var ownerMembership = OrganizationMember.CreateOwner(organization.Id, ownerId, _now);
        var targetMembership = OrganizationMember.Create(organization.Id, targetId, OrganizationRole.OrganizationMember, ownerId, _now);

        _organizations.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        _members.Setup(x => x.GetAsync(organization.Id, ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(ownerMembership);
        _members.Setup(x => x.GetAsync(organization.Id, targetId, It.IsAny<CancellationToken>())).ReturnsAsync(targetMembership);
        _users.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ownerUser, deletedTargetUser });

        var handler = new TransferOrganizationOwnershipCommandHandler(
            _organizations.Object,
            _members.Object,
            _projectMembers.Object,
            _unitOfWork.Object,
            _users.Object,
            _auditWriter.Object,
            _clock.Object);

        var result = await handler.Handle(
            new TransferOrganizationOwnershipCommand(ownerId, organization.Id, targetId, "organizationAdmin", 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("MEMBERSHIP_NOT_FOUND", result.Error?.Code);
    }

    private CreateOrganizationInvitationCommandHandler CreateInvitationHandler()
        => new(
            _organizations.Object,
            _members.Object,
            _projectMembers.Object,
            _unitOfWork.Object,
            _invitations.Object,
            _idempotency.Object,
            _secretHash.Object,
            _frontendUrls.Object,
            _emailOutbox.Object,
            _auditWriter.Object,
            _quotaPolicy.Object,
            _clock.Object);
}
