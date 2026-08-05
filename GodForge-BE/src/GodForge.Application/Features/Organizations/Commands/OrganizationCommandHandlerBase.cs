using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;

namespace GodForge.Application.Features.Organizations.Commands;

public abstract class OrganizationCommandHandlerBase
{
    protected readonly IOrganizationRepository _organizations;
    protected readonly IOrganizationMemberRepository _members;
    protected readonly IProjectMemberRepository _projectMembers;
    protected readonly IUnitOfWork _unitOfWork;

    protected OrganizationCommandHandlerBase(
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members,
        IProjectMemberRepository projectMembers,
        IUnitOfWork unitOfWork)
    {
        _organizations = organizations;
        _members = members;
        _projectMembers = projectMembers;
        _unitOfWork = unitOfWork;
    }

    protected async Task<(Organization? Organization, OrganizationMember? Membership, ApplicationError? Error)> GetActiveAccessAsync(
        Guid actorId, Guid organizationId, string permission, CancellationToken cancellationToken)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken);
        var membership = await _members.GetAsync(organizationId, actorId, cancellationToken);
        if (organization is null || organization.Status == OrganizationStatus.Deleted || membership is not { Status: MembershipStatus.Active })
            return (null, null, ApplicationError.NotFound("ORGANIZATION_NOT_FOUND", "Organization was not found."));
        if (!OrganizationRolePermissions.GetPermissionsForRole(membership.Role).Contains(permission))
            return (null, null, ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You do not have permission for this organization operation."));
        if (organization.Status != OrganizationStatus.Active && permission != Permissions.OrganizationsRead)
            return (null, null, ApplicationError.Conflict("ORGANIZATION_NOT_ACTIVE", "Organization is not active."));
        return (organization, membership, null);
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
        catch (UniqueConstraintConflictException exception) when (exception.ConstraintName == "ux_organizations_slug")
        {
            return ApplicationError.Conflict("ORGANIZATION_SLUG_EXISTS", "Organization slug already exists.");
        }
        catch (UniqueConstraintConflictException exception) when (exception.ConstraintName == "ux_user_invites_active_org_email")
        {
            return ApplicationError.Conflict("INVITE_ALREADY_PENDING", "An active invitation already exists for this email.");
        }
        catch (UniqueConstraintConflictException exception) when (exception.ConstraintName == "ux_idempotency_records_scope")
        {
            return ApplicationError.Conflict("IDEMPOTENCY_KEY_REUSED", "The idempotency key is already being processed or was used previously.");
        }
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

    protected async Task BeginMembershipMutationAsync(string resourceType, Guid resourceId, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        await _unitOfWork.AcquireResourceLockAsync(resourceType, resourceId, cancellationToken);
        _unitOfWork.ClearTrackedChanges();
    }

    protected async Task<IReadOnlyList<Guid>> LockAffectedProjectsAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var memberships = await _projectMembers.GetActiveByOrganizationUserAsync(organizationId, userId, cancellationToken);
        var projectIds = memberships.Select(membership => membership.ProjectId).Distinct().OrderBy(id => id).ToArray();
        foreach (var projectId in projectIds)
            await _unitOfWork.AcquireResourceLockAsync("project-membership", projectId, cancellationToken);
        return projectIds;
    }

    protected static bool CanManage(OrganizationMember actor, OrganizationMember target, OrganizationRole requestedRole)
    {
        if (actor.Role == OrganizationRole.OrganizationOwner) return true;
        if (actor.Role != OrganizationRole.OrganizationAdmin) return false;
        return target.Role != OrganizationRole.OrganizationOwner &&
               target.Role != OrganizationRole.OrganizationAdmin &&
               requestedRole == OrganizationRole.OrganizationMember;
    }
}
