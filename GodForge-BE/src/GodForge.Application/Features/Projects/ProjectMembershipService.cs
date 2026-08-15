using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Common.Text;
using GodForge.Application.Features.Projects.DTOs;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;

namespace GodForge.Application.Features.Projects;

public sealed class ProjectMembershipService : ProjectOperationServiceBase, IProjectMembershipService
{
    private readonly IUserRepository _users;

    public ProjectMembershipService(
        IProjectRepository projects,
        IProjectMemberRepository members,
        IOrganizationMemberRepository organizationMembers,
        IUserRepository users,
        IAuditWriter auditWriter,
        IClock clock,
        IUnitOfWork unitOfWork)
        : base(projects, members, organizationMembers, auditWriter, clock, unitOfWork)
    {
        _users = users;
    }

    public async Task<Result<ProjectOwnershipTransferDto>> TransferOwnershipAsync(Guid actorId, Guid projectId, Guid newOwnerUserId, string retainCurrentOwnerAs, long version, CancellationToken cancellationToken)
    {
        if (actorId == newOwnerUserId)
            return ApplicationError.Validation("VALIDATION_ERROR", "The target user is already the current owner.");
        if (!EnumText.TryParseDefined<ProjectRole>(retainCurrentOwnerAs, out var retainedRole) || retainedRole == ProjectRole.ProjectOwner)
            return ApplicationError.Validation("VALIDATION_ERROR", "retainCurrentOwnerAs must be maintainer, developer, reviewer or viewer.");

        await BeginSerializedMutationAsync("project-membership", projectId, cancellationToken);
        try
        {
            var access = await GetProjectAccessAsync(actorId, projectId, Permissions.ProjectMembersTransferOwnership, false, cancellationToken);
            if (access.Error is not null)
                return await RollbackAsync<ProjectOwnershipTransferDto>(access.Error, cancellationToken);
            if (access.Project!.Version != version)
                return await RollbackAsync<ProjectOwnershipTransferDto>(
                    ApplicationError.Conflict("CONCURRENCY_CONFLICT", "Project version is stale."),
                    cancellationToken);

            var target = await _members.GetMembershipAsync(projectId, newOwnerUserId, cancellationToken);
            var targetOrganizationMembership = await _organizationMembers.GetAsync(access.Project.OrganizationId, newOwnerUserId, cancellationToken);
            var users = await _users.GetByIdsAsync(new[] { actorId, newOwnerUserId }, cancellationToken);
            var usersById = users.ToDictionary(user => user.Id);
            if (target is null ||
                targetOrganizationMembership is not { Status: MembershipStatus.Active } ||
                !usersById.TryGetValue(newOwnerUserId, out var targetUser) ||
                !usersById.TryGetValue(actorId, out var actorUser))
            {
                return await RollbackAsync<ProjectOwnershipTransferDto>(
                    ApplicationError.NotFound("MEMBERSHIP_NOT_FOUND", "Target project membership was not found."),
                    cancellationToken);
            }

            var actorMembership = access.ProjectMembership!;
            var now = _clock.UtcNow;
            target.UpdateRole(ProjectRole.ProjectOwner, target.Version, now);
            actorMembership.UpdateRole(retainedRole, actorMembership.Version, now);
            await _auditWriter.WriteAuditAsync(
                actorId, projectId, "project.ownership_transferred", "project", projectId, "succeeded",
                new { PreviousOwnerUserId = actorId, NewOwnerUserId = newOwnerUserId, RetainedRole = retainedRole.ToString() }, cancellationToken);
            var save = await SaveAsync(cancellationToken);
            if (save is not null)
                return await RollbackAsync<ProjectOwnershipTransferDto>(save, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return new ProjectOwnershipTransferDto(
                ProjectMemberDto.From(actorMembership, actorUser),
                ProjectMemberDto.From(target, targetUser));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
    }

    public async Task<Result<PagedResult<ProjectMemberDto>>> ListMembersAsync(Guid actorId, Guid projectId, int page, int pageSize, string? role, string? status, string? search, CancellationToken cancellationToken)
    {
        if (!ValidPage(page, pageSize)) return InvalidPage();
        var access = await GetProjectAccessAsync(actorId, projectId, Permissions.ProjectMembersRead, false, cancellationToken);
        if (access.Error is not null) return access.Error;
        if (!string.IsNullOrWhiteSpace(role) && !EnumText.TryParseDefined<ProjectRole>(role, out _))
            return ApplicationError.Validation("VALIDATION_ERROR", "Project role is invalid.");
        if (!string.IsNullOrWhiteSpace(status) && !EnumText.TryParseDefined<MembershipStatus>(status, out _))
            return ApplicationError.Validation("VALIDATION_ERROR", "Membership status is invalid.");
        var members = await _members.GetForProjectAsync(projectId, page, pageSize, role, status, search, cancellationToken);
        var users = await _users.GetByIdsAsync(members.Items.Select(member => member.UserId).Distinct().ToArray(), cancellationToken);
        var usersById = users.ToDictionary(user => user.Id);
        var items = members.Items
            .Where(member => usersById.ContainsKey(member.UserId))
            .Select(member => ProjectMemberDto.From(member, usersById[member.UserId]))
            .ToList();
        return new PagedResult<ProjectMemberDto>(items, members.Page, members.PageSize, members.TotalItems);
    }

    public async Task<Result<ProjectMemberDto>> AddMemberAsync(Guid actorId, Guid projectId, Guid userId, string role, CancellationToken cancellationToken)
    {
        if (!EnumText.TryParseDefined<ProjectRole>(role, out var newRole))
            return ApplicationError.Validation("VALIDATION_ERROR", "Project role is invalid.");

        await BeginSerializedMutationAsync("project-membership", projectId, cancellationToken);
        try
        {
            var access = await GetProjectAccessAsync(actorId, projectId, Permissions.ProjectMembersAdd, false, cancellationToken);
            if (access.Error is not null)
                return await RollbackAsync<ProjectMemberDto>(access.Error, cancellationToken);
            if (!CanGrant(access.ProjectMembership!.Role, newRole))
                return await RollbackAsync<ProjectMemberDto>(
                    ApplicationError.Forbidden("SECURITY_FORBIDDEN", "The requested project role is outside the actor's grant boundary."),
                    cancellationToken);

            var organizationMembership = await _organizationMembers.GetAsync(access.Project!.OrganizationId, userId, cancellationToken);
            var user = await _users.GetByIdAsync(userId, cancellationToken);
            if (organizationMembership is not { Status: MembershipStatus.Active } || user is null)
                return await RollbackAsync<ProjectMemberDto>(
                    ApplicationError.NotFound("MEMBERSHIP_NOT_FOUND", "Target user is not an active organization member."),
                    cancellationToken);

            var existing = await _members.GetAnyMembershipAsync(projectId, userId, cancellationToken);
            var now = _clock.UtcNow;
            if (existing is null)
            {
                existing = ProjectMember.Create(
                    projectId,
                    access.Project.OrganizationId,
                    userId,
                    newRole,
                    ProjectMemberSource.Direct,
                    actorId,
                    now);
                await _members.AddAsync(existing, cancellationToken);
            }
            else if (existing.Status == MembershipStatus.Active)
            {
                return await RollbackAsync<ProjectMemberDto>(
                    ApplicationError.Conflict("MEMBERSHIP_ALREADY_EXISTS", "Project membership already exists."),
                    cancellationToken);
            }
            else
            {
                existing.Reactivate(newRole, now);
            }

            await _auditWriter.WriteAuditAsync(
                actorId,
                projectId,
                "project.member_added",
                "project-member",
                existing.Id,
                "succeeded",
                new { userId, Role = newRole.ToString(), existing.Version },
                cancellationToken);

            var save = await SaveAsync(cancellationToken);
            if (save is not null)
                return await RollbackAsync<ProjectMemberDto>(save, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return ProjectMemberDto.From(existing, user);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
    }

    public async Task<Result<ProjectMemberDto>> UpdateMemberAsync(Guid actorId, Guid projectId, Guid userId, string role, long version, CancellationToken cancellationToken)
    {
        if (!EnumText.TryParseDefined<ProjectRole>(role, out var newRole))
            return ApplicationError.Validation("VALIDATION_ERROR", "Project role is invalid.");

        await BeginSerializedMutationAsync("project-membership", projectId, cancellationToken);
        try
        {
            var access = await GetProjectAccessAsync(actorId, projectId, Permissions.ProjectMembersUpdateRole, false, cancellationToken);
            if (access.Error is not null)
                return await RollbackAsync<ProjectMemberDto>(access.Error, cancellationToken);

            var target = await _members.GetAnyMembershipAsync(projectId, userId, cancellationToken);
            var user = await _users.GetByIdAsync(userId, cancellationToken);
            if (target is null || user is null)
                return await RollbackAsync<ProjectMemberDto>(
                    ApplicationError.NotFound("MEMBERSHIP_NOT_FOUND", "Project membership was not found."),
                    cancellationToken);
            if (target.Version != version)
                return await RollbackAsync<ProjectMemberDto>(
                    ApplicationError.Conflict("CONCURRENCY_CONFLICT", "Membership version is stale."),
                    cancellationToken);
            if (!CanManage(access.ProjectMembership!.Role, target.Role, newRole))
                return await RollbackAsync<ProjectMemberDto>(
                    ApplicationError.Forbidden("SECURITY_FORBIDDEN", "The requested project role is outside the actor's grant boundary."),
                    cancellationToken);
            if (target.Role == ProjectRole.ProjectOwner &&
                newRole != ProjectRole.ProjectOwner &&
                await _members.GetOwnerCountAsync(projectId, cancellationToken) <= 1)
            {
                return await RollbackAsync<ProjectMemberDto>(
                    ApplicationError.Conflict("LAST_OWNER_REQUIRED", "At least one active project owner is required."),
                    cancellationToken);
            }

            target.UpdateRole(newRole, version, _clock.UtcNow);
            await _auditWriter.WriteAuditAsync(
                actorId, projectId, "project.member_role_updated", "project-member", target.Id, "succeeded",
                new { userId, Role = newRole.ToString(), target.Version }, cancellationToken);
            var save = await SaveAsync(cancellationToken);
            if (save is not null)
                return await RollbackAsync<ProjectMemberDto>(save, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return ProjectMemberDto.From(target, user);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
    }

    public async Task<Result> RemoveMemberAsync(Guid actorId, Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        await BeginSerializedMutationAsync("project-membership", projectId, cancellationToken);
        try
        {
            var project = await _projects.GetByIdAsync(projectId, cancellationToken);
            var organizationMembership = project is null
                ? null
                : await _organizationMembers.GetAsync(project.OrganizationId, actorId, cancellationToken);
            var actorMembership = await _members.GetMembershipAsync(projectId, actorId, cancellationToken);
            var target = await _members.GetAnyMembershipAsync(projectId, userId, cancellationToken);
            if (project is null || organizationMembership is not { Status: MembershipStatus.Active } || actorMembership is null || target is null)
                return await RollbackAsync(
                    ApplicationError.NotFound("PROJECT_NOT_FOUND", "Project was not found."),
                    cancellationToken);
            if (actorId != userId && !RolePermissions.GetPermissionsForRole(actorMembership.Role).Contains(Permissions.ProjectMembersRemove))
                return await RollbackAsync(
                    ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You cannot remove this project member."),
                    cancellationToken);
            if (actorId != userId && !CanManage(actorMembership.Role, target.Role, ProjectRole.Viewer))
                return await RollbackAsync(
                    ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You cannot remove this project member."),
                    cancellationToken);
            if (target.Status == MembershipStatus.Removed)
                return await RollbackAsync(
                    ApplicationError.NotFound("MEMBERSHIP_NOT_FOUND", "Project membership was not found."),
                    cancellationToken);
            if (target.Role == ProjectRole.ProjectOwner &&
                target.Status == MembershipStatus.Active &&
                await _members.GetOwnerCountAsync(projectId, cancellationToken) <= 1)
            {
                return await RollbackAsync(
                    ApplicationError.Conflict("LAST_OWNER_REQUIRED", "At least one active project owner is required."),
                    cancellationToken);
            }

            target.Remove(_clock.UtcNow);
            await _auditWriter.WriteAuditAsync(
                actorId, projectId, "project.member_removed", "project-member", target.Id, "succeeded",
                new { userId, target.Version }, cancellationToken);
            var save = await SaveAsync(cancellationToken);
            if (save is not null)
                return await RollbackAsync(save, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return Result.Success();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
    }

    private static bool CanGrant(ProjectRole actorRole, ProjectRole requestedRole) => actorRole switch
    {
        ProjectRole.ProjectOwner => true,
        ProjectRole.Maintainer => requestedRole != ProjectRole.ProjectOwner,
        _ => false
    };

    private static bool CanManage(ProjectRole actorRole, ProjectRole targetRole, ProjectRole requestedRole) => actorRole switch
    {
        ProjectRole.ProjectOwner => true,
        ProjectRole.Maintainer => targetRole != ProjectRole.ProjectOwner && requestedRole != ProjectRole.ProjectOwner,
        _ => false
    };
}
