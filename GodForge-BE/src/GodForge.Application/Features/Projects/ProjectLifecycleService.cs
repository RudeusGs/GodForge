using GodForge.Application.Common.Idempotency;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Common.Text;
using GodForge.Application.Features.Projects.DTOs;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Enums;

namespace GodForge.Application.Features.Projects;

public sealed class ProjectLifecycleService : ProjectOperationServiceBase, IProjectLifecycleService
{
    private readonly IOrganizationRepository _organizations;
    private readonly IIdempotencyRepository _idempotency;
    private readonly IM1QuotaPolicy _quotaPolicy;

    public ProjectLifecycleService(
        IProjectRepository projects,
        IProjectMemberRepository members,
        IOrganizationRepository organizations,
        IOrganizationMemberRepository organizationMembers,
        IIdempotencyRepository idempotency,
        IAuditWriter auditWriter,
        IM1QuotaPolicy quotaPolicy,
        IClock clock,
        IUnitOfWork unitOfWork)
        : base(projects, members, organizationMembers, auditWriter, clock, unitOfWork)
    {
        _organizations = organizations;
        _idempotency = idempotency;
        _quotaPolicy = quotaPolicy;
    }

    public async Task<Result<PagedResult<ProjectDto>>> ListAsync(Guid actorId, int page, int pageSize, Guid? organizationId, string? status, string? search, CancellationToken cancellationToken)
    {
        if (!ValidPage(page, pageSize)) return InvalidPage();
        if (!string.IsNullOrWhiteSpace(status) && !EnumText.TryParseDefined<ProjectStatus>(status, out _))
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
        if (idempotencyError is not null)
            return idempotencyError;

        var requestHash = IdempotencyRequest.Hash($"{organizationId:N}\n{name.Trim()}\n{slug.Trim()}\n{description?.Trim()}\n{visibility.Trim().ToLowerInvariant()}");
        if (normalizedIdempotencyKey is not null)
        {
            var existingResult = await GetExistingCreateResultAsync(
                actorId,
                normalizedIdempotencyKey,
                requestHash,
                cancellationToken);
            if (existingResult is not null)
                return existingResult;
        }

        await BeginSerializedMutationAsync("organization-project-catalog", organizationId, cancellationToken);
        try
        {
            organization = await _organizations.GetByIdAsync(organizationId, cancellationToken);
            organizationMembership = await _organizationMembers.GetAsync(organizationId, actorId, cancellationToken);
            if (organization is null || organization.Status != OrganizationStatus.Active || organizationMembership is not { Status: MembershipStatus.Active })
                return await RollbackAsync<ProjectDto>(
                    ApplicationError.NotFound("ORGANIZATION_NOT_FOUND", "Organization was not found."),
                    cancellationToken);
            if (!OrganizationRolePermissions.GetPermissionsForRole(organizationMembership.Role).Contains(Permissions.OrganizationProjectsCreate))
                return await RollbackAsync<ProjectDto>(
                    ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You cannot create projects in this organization."),
                    cancellationToken);

            if (normalizedIdempotencyKey is not null)
            {
                var existingResult = await GetExistingCreateResultAsync(
                    actorId,
                    normalizedIdempotencyKey,
                    requestHash,
                    cancellationToken);
                if (existingResult is not null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    _unitOfWork.ClearTrackedChanges();
                    return existingResult;
                }
            }

            if (await _projects.CountForOrganizationAsync(organizationId, cancellationToken) >= _quotaPolicy.MaxProjectsPerOrganization)
                return await RollbackAsync<ProjectDto>(
                    ApplicationError.TooManyRequests(
                        "PROJECT_QUOTA_EXCEEDED",
                        "The organization project quota has been reached.",
                        new { limit = _quotaPolicy.MaxProjectsPerOrganization }),
                    cancellationToken);
            if (await _projects.NameExistsAsync(organizationId, name.Trim(), cancellationToken: cancellationToken))
                return await RollbackAsync<ProjectDto>(
                    ApplicationError.Conflict("PROJECT_NAME_EXISTS", "A project with this name already exists in the organization."),
                    cancellationToken);
            if (await _projects.SlugExistsAsync(organizationId, slug, cancellationToken: cancellationToken))
                return await RollbackAsync<ProjectDto>(
                    ApplicationError.Conflict("PROJECT_SLUG_EXISTS", "A project with this slug already exists in the organization."),
                    cancellationToken);

            var now = _clock.UtcNow;
            var project = Project.Create(
                organizationId,
                name,
                slug,
                description,
                Project.UnknownGodotVersion,
                parsedVisibility,
                actorId,
                now);
            var membership = ProjectMember.Create(
                project.Id,
                organizationId,
                actorId,
                ProjectRole.ProjectOwner,
                ProjectMemberSource.Direct,
                actorId,
                now);
            var settings = ProjectSetting.Create(project.Id, now);

            await _projects.AddAsync(project, cancellationToken);
            await _members.AddAsync(membership, cancellationToken);
            await _projects.AddSettingsAsync(settings, cancellationToken);
            if (normalizedIdempotencyKey is not null)
            {
                await _idempotency.AddAsync(IdempotencyRecord.Create(
                    actorId,
                    "project.create",
                    normalizedIdempotencyKey,
                    requestHash,
                    "project",
                    project.Id,
                    now), cancellationToken);
            }

            await _auditWriter.WriteAuditAsync(
                actorId,
                project.Id,
                "project.created",
                "project",
                project.Id,
                "succeeded",
                new { organizationId, project.Name, project.Slug, Visibility = project.Visibility.ToString() },
                cancellationToken);

            var save = await SaveAsync(cancellationToken);
            if (save is not null)
                return await RollbackAsync<ProjectDto>(save, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return ProjectDto.From(project, membership);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
    }

    public async Task<Result<ProjectDto>> GetAsync(Guid actorId, Guid projectId, CancellationToken cancellationToken)
    {
        var access = await GetProjectAccessAsync(actorId, projectId, Permissions.ProjectsRead, false, cancellationToken);
        if (access.Error is not null) return access.Error;
        return ProjectDto.From(access.Project!, access.ProjectMembership!);
    }

    public async Task<Result<ProjectDto>> UpdateAsync(Guid actorId, Guid projectId, string name, string slug, string? description, string visibility, long version, CancellationToken cancellationToken)
    {
        var initialAccess = await GetProjectAccessAsync(actorId, projectId, Permissions.ProjectsUpdate, false, cancellationToken);
        if (initialAccess.Error is not null)
            return initialAccess.Error;
        if (!ValidateProjectFields(name, slug, visibility, out var parsedVisibility))
            return ApplicationError.Validation("VALIDATION_ERROR", "Project name, slug or visibility is invalid.");

        await BeginSerializedMutationAsync("organization-project-catalog", initialAccess.Project!.OrganizationId, cancellationToken);
        try
        {
            var access = await GetProjectAccessAsync(actorId, projectId, Permissions.ProjectsUpdate, false, cancellationToken);
            if (access.Error is not null)
                return await RollbackAsync<ProjectDto>(access.Error, cancellationToken);

            var project = access.Project!;
            if (project.Version != version)
                return await RollbackAsync<ProjectDto>(
                    ApplicationError.Conflict("CONCURRENCY_CONFLICT", "Project version is stale."),
                    cancellationToken);
            if (await _projects.SlugExistsAsync(project.OrganizationId, slug, project.Id, cancellationToken))
                return await RollbackAsync<ProjectDto>(
                    ApplicationError.Conflict("PROJECT_SLUG_EXISTS", "A project with this slug already exists in the organization."),
                    cancellationToken);
            if (!string.Equals(project.Name, name.Trim(), StringComparison.Ordinal) &&
                await _projects.NameExistsAsync(project.OrganizationId, name.Trim(), project.Id, cancellationToken))
            {
                return await RollbackAsync<ProjectDto>(
                    ApplicationError.Conflict("PROJECT_NAME_EXISTS", "A project with this name already exists in the organization."),
                    cancellationToken);
            }

            project.UpdateDetails(name, slug, description, parsedVisibility, version, _clock.UtcNow);
            await _auditWriter.WriteAuditAsync(
                actorId,
                project.Id,
                "project.updated",
                "project",
                project.Id,
                "succeeded",
                new { project.Name, project.Slug, Visibility = project.Visibility.ToString(), project.Version },
                cancellationToken);

            var save = await SaveAsync(cancellationToken);
            if (save is not null)
                return await RollbackAsync<ProjectDto>(save, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return ProjectDto.From(project, access.ProjectMembership!);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
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

    private async Task<Result<ProjectDto>?> GetExistingCreateResultAsync(
        Guid actorId,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var existingRecord = await _idempotency.GetAsync(
            actorId,
            "project.create",
            idempotencyKey,
            cancellationToken);
        if (existingRecord is null)
            return null;

        if (!string.Equals(existingRecord.RequestHash, requestHash, StringComparison.Ordinal))
            return ApplicationError.Conflict(
                "IDEMPOTENCY_KEY_REUSED",
                "The idempotency key was already used with a different request.");

        var existingProject = await _projects.GetByIdAsync(existingRecord.ResourceId, cancellationToken);
        var existingMembership = await _members.GetMembershipAsync(
            existingRecord.ResourceId,
            actorId,
            cancellationToken);
        if (existingProject is not null && existingMembership is not null)
            return ProjectDto.From(existingProject, existingMembership);

        return ApplicationError.Conflict(
            "IDEMPOTENCY_RESOURCE_UNAVAILABLE",
            "The resource recorded for this idempotency key is unavailable.");
    }

    private static bool ValidateProjectFields(string name, string slug, string visibility, out ProjectVisibility parsedVisibility)
    {
        parsedVisibility = default;
        return Project.IsValidName(name) &&
               Project.IsValidSlug(slug) &&
               EnumText.TryParseDefined(visibility, out parsedVisibility);
    }
}
