using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Idempotency;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Common.Text;
using GodForge.Application.Features.Projects.DTOs;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Enums;

namespace GodForge.Application.Features.Projects;

public sealed class ProjectManagementService : IProjectManagementService
{
    private readonly IProjectRepository _projects;
    private readonly IProjectMemberRepository _members;
    private readonly IOrganizationRepository _organizations;
    private readonly IOrganizationMemberRepository _organizationMembers;
    private readonly IUserRepository _users;
    private readonly IIdempotencyRepository _idempotency;
    private readonly IAuditWriter _auditWriter;
    private readonly IM1QuotaPolicy _quotaPolicy;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public ProjectManagementService(
        IProjectRepository projects,
        IProjectMemberRepository members,
        IOrganizationRepository organizations,
        IOrganizationMemberRepository organizationMembers,
        IUserRepository users,
        IIdempotencyRepository idempotency,
        IAuditWriter auditWriter,
        IM1QuotaPolicy quotaPolicy,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _members = members;
        _organizations = organizations;
        _organizationMembers = organizationMembers;
        _users = users;
        _idempotency = idempotency;
        _auditWriter = auditWriter;
        _quotaPolicy = quotaPolicy;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<ProjectDto>>> ListAsync(Guid actorId, int page, int pageSize, Guid? organizationId, string? status, string? search, CancellationToken cancellationToken)
    {
        if (!ValidPage(page, pageSize)) return InvalidPage();
        if (!string.IsNullOrWhiteSpace(status) && !Enum.TryParse<ProjectStatus>(status, true, out _))
            return ApplicationError.Validation("VALIDATION_ERROR", "Project status is invalid.");
        var projects = await _projects.GetVisibleProjectsAsync(actorId, page, pageSize, search, organizationId, status, cancellationToken);
        var projectIds = projects.Items.Select(project => project.Id).ToArray();
        var memberships = await _members.GetMembershipsAsync(projectIds, actorId, cancellationToken);
        var membershipByProject = memberships.ToDictionary(membership => membership.ProjectId);
        var items = projects.Items
            .Where(project => membershipByProject.ContainsKey(project.Id))
            .Select(project => ProjectDto.From(project, membershipByProject[project.Id]))
            .ToList();
        return new PagedResult<ProjectDto>(items, projects.Page, projects.PageSize, projects.TotalItems);
    }

    public async Task<Result<PagedResult<ProjectAdministrationDto>>> ListForOrganizationAsync(Guid actorId, Guid organizationId, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!ValidPage(page, pageSize)) return InvalidPage();
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken);
        var organizationMembership = await _organizationMembers.GetAsync(organizationId, actorId, cancellationToken);
        if (organization is null || organizationMembership is not { Status: MembershipStatus.Active })
            return ApplicationError.NotFound("ORGANIZATION_NOT_FOUND", "Organization was not found.");
        var includeAll = organizationMembership.Role is OrganizationRole.OrganizationOwner or OrganizationRole.OrganizationAdmin;
        var projects = await _projects.GetForOrganizationAsync(organizationId, actorId, includeAll, page, pageSize, cancellationToken);
        var projectIds = projects.Items.Select(project => project.Id).ToArray();
        var statistics = await _members.GetStatisticsAsync(projectIds, cancellationToken);
        var statisticsByProject = statistics.ToDictionary(item => item.ProjectId);
        var items = projects.Items.Select(project =>
        {
            statisticsByProject.TryGetValue(project.Id, out var memberStatistics);
            return new ProjectAdministrationDto(
                project.Id,
                project.OrganizationId,
                project.Slug,
                project.Name,
                EnumText.ToCamelCase(project.Status),
                memberStatistics?.OwnerCount ?? 0,
                memberStatistics?.ActiveMemberCount ?? 0,
                project.CreatedAt,
                project.Version);
        }).ToList();
        return new PagedResult<ProjectAdministrationDto>(items, projects.Page, projects.PageSize, projects.TotalItems);
    }

    public async Task<Result<ProjectDto>> CreateAsync(Guid actorId, Guid organizationId, string name, string slug, string? description, string visibility, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken);
        var organizationMembership = await _organizationMembers.GetAsync(organizationId, actorId, cancellationToken);
        if (organization is null || organization.Status != OrganizationStatus.Active || organizationMembership is not { Status: MembershipStatus.Active })
            return ApplicationError.NotFound("ORGANIZATION_NOT_FOUND", "Organization was not found.");
        if (!OrganizationRolePermissions.GetPermissionsForRole(organizationMembership.Role).Contains(Permissions.OrganizationProjectsCreate))
            return ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You cannot create projects in this organization.");
        if (!ValidateProjectFields(name, slug, visibility, out var parsedVisibility))
            return ApplicationError.Validation("VALIDATION_ERROR", "Project name, slug or visibility is invalid.");
        var idempotencyError = IdempotencyRequest.Normalize(idempotencyKey, out var normalizedIdempotencyKey);
        if (idempotencyError is not null) return idempotencyError;
        var requestHash = IdempotencyRequest.Hash($"{organizationId:N}\n{name.Trim()}\n{slug.Trim()}\n{description?.Trim()}\n{visibility.Trim().ToLowerInvariant()}");
        if (normalizedIdempotencyKey is not null)
        {
            var existingRecord = await _idempotency.GetAsync(actorId, "project.create", normalizedIdempotencyKey, cancellationToken);
            if (existingRecord is not null)
            {
                if (!string.Equals(existingRecord.RequestHash, requestHash, StringComparison.Ordinal))
                    return ApplicationError.Conflict("IDEMPOTENCY_KEY_REUSED", "The idempotency key was already used with a different request.");
                var existingProject = await _projects.GetByIdAsync(existingRecord.ResourceId, cancellationToken);
                var existingMembership = await _members.GetMembershipAsync(existingRecord.ResourceId, actorId, cancellationToken);
                if (existingProject is not null && existingMembership is not null)
                    return ProjectDto.From(existingProject, existingMembership);
                return ApplicationError.Conflict("IDEMPOTENCY_RESOURCE_UNAVAILABLE", "The resource recorded for this idempotency key is unavailable.");
            }
        }
        if (await _projects.CountForOrganizationAsync(organizationId, cancellationToken) >= _quotaPolicy.MaxProjectsPerOrganization)
            return ApplicationError.TooManyRequests(
                "PROJECT_QUOTA_EXCEEDED",
                "The organization project quota has been reached.",
                new { limit = _quotaPolicy.MaxProjectsPerOrganization });
        if (await _projects.NameExistsAsync(organizationId, name.Trim(), cancellationToken))
            return ApplicationError.Conflict("PROJECT_NAME_EXISTS", "A project with this name already exists in the organization.");
        if (await _projects.SlugExistsAsync(organizationId, slug, cancellationToken: cancellationToken))
            return ApplicationError.Conflict("PROJECT_SLUG_EXISTS", "A project with this slug already exists in the organization.");

        var now = _clock.UtcNow;
        var project = Project.Create(organizationId, name, slug, description, "4.3", parsedVisibility, actorId, now);
        var membership = ProjectMember.Create(project.Id, organizationId, actorId, ProjectRole.ProjectOwner, ProjectMemberSource.Direct, actorId, now);
        var settings = ProjectSetting.Create(project.Id, now);
        await _projects.AddAsync(project, cancellationToken);
        await _members.AddAsync(membership, cancellationToken);
        await _projects.AddSettingsAsync(settings, cancellationToken);
        if (normalizedIdempotencyKey is not null)
            await _idempotency.AddAsync(IdempotencyRecord.Create(
                actorId, "project.create", normalizedIdempotencyKey, requestHash, "project", project.Id, now), cancellationToken);
        await _auditWriter.WriteAuditAsync(
            actorId, project.Id, "project.created", "project", project.Id, "succeeded",
            new { organizationId, project.Name, project.Slug, Visibility = project.Visibility.ToString() }, cancellationToken);
        var save = await SaveAsync(cancellationToken);
        if (save is not null) return save;
        return ProjectDto.From(project, membership);
    }

    public async Task<Result<ProjectDto>> GetAsync(Guid actorId, Guid projectId, CancellationToken cancellationToken)
    {
        var access = await GetProjectAccessAsync(actorId, projectId, Permissions.ProjectsRead, false, cancellationToken);
        if (access.Error is not null) return access.Error;
        return ProjectDto.From(access.Project!, access.ProjectMembership!);
    }

    public async Task<Result<ProjectDto>> UpdateAsync(Guid actorId, Guid projectId, string name, string slug, string? description, string visibility, long version, CancellationToken cancellationToken)
    {
        var access = await GetProjectAccessAsync(actorId, projectId, Permissions.ProjectsUpdate, false, cancellationToken);
        if (access.Error is not null) return access.Error;
        var project = access.Project!;
        if (!ValidateProjectFields(name, slug, visibility, out var parsedVisibility))
            return ApplicationError.Validation("VALIDATION_ERROR", "Project name, slug or visibility is invalid.");
        if (project.Version != version)
            return ApplicationError.Conflict("CONCURRENCY_CONFLICT", "Project version is stale.");
        if (await _projects.SlugExistsAsync(project.OrganizationId, slug, project.Id, cancellationToken))
            return ApplicationError.Conflict("PROJECT_SLUG_EXISTS", "A project with this slug already exists in the organization.");
        if (!string.Equals(project.Name, name.Trim(), StringComparison.Ordinal) &&
            await _projects.NameExistsAsync(project.OrganizationId, name.Trim(), cancellationToken))
            return ApplicationError.Conflict("PROJECT_NAME_EXISTS", "A project with this name already exists in the organization.");

        project.UpdateDetails(name, slug, description, parsedVisibility, version, _clock.UtcNow);
        await _auditWriter.WriteAuditAsync(
            actorId, project.Id, "project.updated", "project", project.Id, "succeeded",
            new { project.Name, project.Slug, Visibility = project.Visibility.ToString(), project.Version }, cancellationToken);
        var save = await SaveAsync(cancellationToken);
        if (save is not null) return save;
        return ProjectDto.From(project, access.ProjectMembership!);
    }

    public async Task<Result<ProjectDeletionAcceptedDto>> RequestDeletionAsync(Guid actorId, Guid projectId, long version, string confirmationSlug, CancellationToken cancellationToken)
    {
        var access = await GetProjectAccessAsync(actorId, projectId, Permissions.ProjectsDelete, true, cancellationToken);
        if (access.Error is not null) return access.Error;
        var project = access.Project!;
        if (!string.Equals(project.Slug, confirmationSlug?.Trim(), StringComparison.Ordinal))
            return ApplicationError.Validation("VALIDATION_ERROR", "confirmationSlug does not match the project slug.");
        if (project.Version != version)
            return ApplicationError.Conflict("CONCURRENCY_CONFLICT", "Project version is stale.");
        project.MarkDeleting(version, _clock.UtcNow);
        await _auditWriter.WriteAuditAsync(
            actorId, project.Id, "project.deletion_requested", "project", project.Id, "succeeded",
            new { project.Slug, project.Version }, cancellationToken);
        var save = await SaveAsync(cancellationToken);
        if (save is not null) return save;
        return new ProjectDeletionAcceptedDto(project.Id, "deleting", project.Version);
    }

    public async Task<Result<ProjectDto>> RestoreAsync(Guid actorId, Guid projectId, long version, CancellationToken cancellationToken)
    {
        var access = await GetProjectAccessAsync(actorId, projectId, Permissions.ProjectsRestore, true, cancellationToken);
        if (access.Error is not null) return access.Error;
        var project = access.Project!;
        if (project.Version != version)
            return ApplicationError.Conflict("CONCURRENCY_CONFLICT", "Project version is stale.");
        try
        {
            project.Restore(version, _clock.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return ApplicationError.Conflict("PROJECT_NOT_ARCHIVED", "Only an archived project can be restored.");
        }
        await _auditWriter.WriteAuditAsync(
            actorId, project.Id, "project.restored", "project", project.Id, "succeeded",
            new { project.Version }, cancellationToken);
        var save = await SaveAsync(cancellationToken);
        if (save is not null) return save;
        if (access.ProjectMembership is not null)
            return ProjectDto.From(project, access.ProjectMembership);
        return ProjectDto.From(project, EnumText.ToCamelCase(access.OrganizationMembership!.Role));
    }

    public async Task<Result<ProjectOwnershipTransferDto>> TransferOwnershipAsync(Guid actorId, Guid projectId, Guid newOwnerUserId, string retainCurrentOwnerAs, long version, CancellationToken cancellationToken)
    {
        if (actorId == newOwnerUserId)
            return ApplicationError.Validation("VALIDATION_ERROR", "The target user is already the current owner.");
        if (!Enum.TryParse<ProjectRole>(retainCurrentOwnerAs, true, out var retainedRole) || retainedRole == ProjectRole.ProjectOwner)
            return ApplicationError.Validation("VALIDATION_ERROR", "retainCurrentOwnerAs must be maintainer, developer, reviewer or viewer.");

        await BeginMembershipMutationAsync("project-membership", projectId, cancellationToken);
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
        if (!string.IsNullOrWhiteSpace(role) && !Enum.TryParse<ProjectRole>(role, true, out _))
            return ApplicationError.Validation("VALIDATION_ERROR", "Project role is invalid.");
        if (!string.IsNullOrWhiteSpace(status) && !Enum.TryParse<MembershipStatus>(status, true, out _))
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
        var access = await GetProjectAccessAsync(actorId, projectId, Permissions.ProjectMembersAdd, false, cancellationToken);
        if (access.Error is not null) return access.Error;
        if (!Enum.TryParse<ProjectRole>(role, true, out var newRole))
            return ApplicationError.Validation("VALIDATION_ERROR", "Project role is invalid.");
        if (!CanGrant(access.ProjectMembership!.Role, newRole))
            return ApplicationError.Forbidden("SECURITY_FORBIDDEN", "The requested project role is outside the actor's grant boundary.");
        var organizationMembership = await _organizationMembers.GetAsync(access.Project!.OrganizationId, userId, cancellationToken);
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (organizationMembership is not { Status: MembershipStatus.Active } || user is null)
            return ApplicationError.NotFound("MEMBERSHIP_NOT_FOUND", "Target user is not an active organization member.");
        var existing = await _members.GetAnyMembershipAsync(projectId, userId, cancellationToken);
        var now = _clock.UtcNow;
        if (existing is null)
        {
            existing = ProjectMember.Create(projectId, access.Project.OrganizationId, userId, newRole, ProjectMemberSource.Direct, actorId, now);
            await _members.AddAsync(existing, cancellationToken);
        }
        else if (existing.Status == MembershipStatus.Active)
        {
            return ApplicationError.Conflict("MEMBERSHIP_ALREADY_EXISTS", "Project membership already exists.");
        }
        else
        {
            existing.Reactivate(newRole, now);
        }
        await _auditWriter.WriteAuditAsync(
            actorId, projectId, "project.member_added", "project-member", existing.Id, "succeeded",
            new { userId, Role = newRole.ToString(), existing.Version }, cancellationToken);
        var save = await SaveAsync(cancellationToken);
        if (save is not null) return save;
        return ProjectMemberDto.From(existing, user);
    }

    public async Task<Result<ProjectMemberDto>> UpdateMemberAsync(Guid actorId, Guid projectId, Guid userId, string role, long version, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ProjectRole>(role, true, out var newRole))
            return ApplicationError.Validation("VALIDATION_ERROR", "Project role is invalid.");

        await BeginMembershipMutationAsync("project-membership", projectId, cancellationToken);
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
        await BeginMembershipMutationAsync("project-membership", projectId, cancellationToken);
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

    public async Task<Result<ProjectSettingsDto>> GetSettingsAsync(Guid actorId, Guid projectId, CancellationToken cancellationToken)
    {
        var access = await GetProjectAccessAsync(actorId, projectId, Permissions.ProjectsRead, false, cancellationToken);
        if (access.Error is not null) return access.Error;
        var settings = await _projects.GetSettingsAsync(projectId, cancellationToken);
        return settings is null
            ? ApplicationError.NotFound("PROJECT_SETTINGS_NOT_FOUND", "Project settings were not found.")
            : ProjectSettingsDto.From(settings);
    }

    public async Task<Result<ProjectSettingsDto>> UpdateSettingsAsync(Guid actorId, Guid projectId, string analysisProfileKey, bool aiAdvisoryEnabled, string defaultAssetVisibility, int notificationPolicyVersion, long version, CancellationToken cancellationToken)
    {
        var access = await GetProjectAccessAsync(actorId, projectId, Permissions.SettingsUpdate, false, cancellationToken);
        if (access.Error is not null) return access.Error;
        if (string.IsNullOrWhiteSpace(analysisProfileKey) || defaultAssetVisibility is not ("private" or "internal") || notificationPolicyVersion < 1)
            return ApplicationError.Validation("VALIDATION_ERROR", "Project settings are invalid.");
        var settings = await _projects.GetSettingsAsync(projectId, cancellationToken);
        if (settings is null)
            return ApplicationError.NotFound("PROJECT_SETTINGS_NOT_FOUND", "Project settings were not found.");
        if (settings.Version != version)
            return ApplicationError.Conflict("CONCURRENCY_CONFLICT", "Project settings version is stale.");
        settings.Update(analysisProfileKey, aiAdvisoryEnabled, defaultAssetVisibility, notificationPolicyVersion, version, _clock.UtcNow);
        await _auditWriter.WriteAuditAsync(
            actorId, projectId, "project.settings_updated", "project-settings", settings.Id, "succeeded",
            new { analysisProfileKey, aiAdvisoryEnabled, defaultAssetVisibility, notificationPolicyVersion, settings.Version }, cancellationToken);
        var save = await SaveAsync(cancellationToken);
        if (save is not null) return save;
        return ProjectSettingsDto.From(settings);
    }

    private async Task BeginMembershipMutationAsync(string resourceType, Guid resourceId, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        await _unitOfWork.AcquireResourceLockAsync(resourceType, resourceId, cancellationToken);
        _unitOfWork.ClearTrackedChanges();
    }

    private async Task<Result<T>> RollbackAsync<T>(ApplicationError error, CancellationToken cancellationToken)
    {
        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        _unitOfWork.ClearTrackedChanges();
        return error;
    }

    private async Task<Result> RollbackAsync(ApplicationError error, CancellationToken cancellationToken)
    {
        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        _unitOfWork.ClearTrackedChanges();
        return Result.Failure(error);
    }

    private async Task<(Project? Project, ProjectMember? ProjectMembership, OrganizationMember? OrganizationMembership, ApplicationError? Error)> GetProjectAccessAsync(
        Guid actorId, Guid projectId, string permission, bool allowOrganizationAdministration, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(projectId, cancellationToken);
        if (project is null || project.Status == ProjectStatus.Deleted || project.DeletedAt is not null)
            return (null, null, null, ApplicationError.NotFound("PROJECT_NOT_FOUND", "Project was not found."));
        var organizationMembership = await _organizationMembers.GetAsync(project.OrganizationId, actorId, cancellationToken);
        if (organizationMembership is not { Status: MembershipStatus.Active })
            return (null, null, null, ApplicationError.NotFound("PROJECT_NOT_FOUND", "Project was not found."));
        var projectMembership = await _members.GetMembershipAsync(projectId, actorId, cancellationToken);
        if (projectMembership is not null && RolePermissions.GetPermissionsForRole(projectMembership.Role).Contains(permission))
            return (project, projectMembership, organizationMembership, null);
        if (allowOrganizationAdministration && organizationMembership.Role is OrganizationRole.OrganizationOwner or OrganizationRole.OrganizationAdmin)
            return (project, projectMembership, organizationMembership, null);
        return (null, null, null, ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You do not have permission for this project operation."));
    }

    private async Task<ApplicationError?> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (ConcurrencyConflictException)
        {
            return ApplicationError.Conflict("CONCURRENCY_CONFLICT", "The resource changed before this operation completed.");
        }
        catch (UniqueConstraintConflictException exception) when (exception.ConstraintName == "ux_projects_org_slug_active")
        {
            return ApplicationError.Conflict("PROJECT_SLUG_EXISTS", "A project with this slug already exists in the organization.");
        }
        catch (UniqueConstraintConflictException exception) when (exception.ConstraintName == "ux_idempotency_records_scope")
        {
            return ApplicationError.Conflict("IDEMPOTENCY_KEY_REUSED", "The idempotency key is already being processed or was used previously.");
        }
    }

    private static bool ValidateProjectFields(string name, string slug, string visibility, out ProjectVisibility parsedVisibility)
    {
        parsedVisibility = default;
        return !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 80 &&
               !string.IsNullOrWhiteSpace(slug) && slug.Length <= 80 &&
               System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$") &&
               Enum.TryParse(visibility, true, out parsedVisibility);
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

    private static bool ValidPage(int page, int pageSize) => page > 0 && pageSize is > 0 and <= 100;
    private static ApplicationError InvalidPage() => ApplicationError.Validation("VALIDATION_ERROR", "page must be positive and pageSize must be between 1 and 100.");
}
