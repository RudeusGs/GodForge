using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;

namespace GodForge.Application.Features.Projects;

public abstract class ProjectOperationServiceBase
{
    protected readonly IProjectRepository _projects;
    protected readonly IProjectMemberRepository _members;
    protected readonly IOrganizationMemberRepository _organizationMembers;
    protected readonly IAuditWriter _auditWriter;
    protected readonly IClock _clock;
    protected readonly IUnitOfWork _unitOfWork;

    protected ProjectOperationServiceBase(
        IProjectRepository projects,
        IProjectMemberRepository members,
        IOrganizationMemberRepository organizationMembers,
        IAuditWriter auditWriter,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _members = members;
        _organizationMembers = organizationMembers;
        _auditWriter = auditWriter;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    protected async Task BeginSerializedMutationAsync(string resourceType, Guid resourceId, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        await _unitOfWork.AcquireResourceLockAsync(resourceType, resourceId, cancellationToken);
        _unitOfWork.ClearTrackedChanges();
    }

    protected async Task<Result<T>> RollbackAsync<T>(ApplicationError error, CancellationToken cancellationToken)
    {
        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        _unitOfWork.ClearTrackedChanges();
        return error;
    }

    protected async Task<Result> RollbackAsync(ApplicationError error, CancellationToken cancellationToken)
    {
        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        _unitOfWork.ClearTrackedChanges();
        return Result.Failure(error);
    }

    protected async Task<(Project? Project, ProjectMember? ProjectMembership, OrganizationMember? OrganizationMembership, ApplicationError? Error)> GetProjectAccessAsync(
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

    protected async Task<ApplicationError?> SaveAsync(CancellationToken cancellationToken)
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
        catch (UniqueConstraintConflictException exception) when (exception.Constraint == UniqueConstraintKind.ProjectOrganizationSlug)
        {
            return ApplicationError.Conflict("PROJECT_SLUG_EXISTS", "A project with this slug already exists in the organization.");
        }
        catch (UniqueConstraintConflictException exception) when (exception.Constraint == UniqueConstraintKind.ProjectOrganizationName)
        {
            return ApplicationError.Conflict("PROJECT_NAME_EXISTS", "A project with this name already exists in the organization.");
        }
        catch (UniqueConstraintConflictException exception) when (exception.Constraint == UniqueConstraintKind.IdempotencyScope)
        {
            return ApplicationError.Conflict("IDEMPOTENCY_KEY_REUSED", "The idempotency key is already being processed or was used previously.");
        }
        catch (UniqueConstraintConflictException exception) when (exception.Constraint == UniqueConstraintKind.ProjectMemberUser)
        {
            return ApplicationError.Conflict("MEMBERSHIP_ALREADY_EXISTS", "Project membership already exists.");
        }
    }

    protected static bool ValidPage(int page, int pageSize) => page > 0 && pageSize is > 0 and <= 100;

    protected static ApplicationError InvalidPage()
        => ApplicationError.Validation("VALIDATION_ERROR", "page must be positive and pageSize must be between 1 and 100.");
}
