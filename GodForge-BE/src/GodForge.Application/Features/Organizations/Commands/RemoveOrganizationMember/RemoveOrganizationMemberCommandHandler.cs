using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.RemoveOrganizationMember;

public sealed class RemoveOrganizationMemberCommandHandler : OrganizationCommandHandlerBase, IRequestHandler<RemoveOrganizationMemberCommand, Result>
{
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;

    public RemoveOrganizationMemberCommandHandler(
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members,
        IProjectMemberRepository projectMembers,
        IUnitOfWork unitOfWork,
        IAuditWriter auditWriter,
        IClock clock) : base(organizations, members, projectMembers, unitOfWork)
    {
        _auditWriter = auditWriter;
        _clock = clock;
    }

    public async Task<Result> Handle(RemoveOrganizationMemberCommand request, CancellationToken cancellationToken)
    {
        await BeginMembershipMutationAsync("organization-membership", request.OrganizationId, cancellationToken);
        try
        {
            var organization = await _organizations.GetByIdAsync(request.OrganizationId, cancellationToken);
            var actorMembership = await _members.GetAsync(request.OrganizationId, request.ActorId, cancellationToken);
            var target = await _members.GetAsync(request.OrganizationId, request.UserId, cancellationToken);
            
            if (organization is null || organization.Status != OrganizationStatus.Active ||
                actorMembership is not { Status: MembershipStatus.Active } || target is null || target.Status == MembershipStatus.Removed)
            {
                return await RollbackAsync(
                    ApplicationError.NotFound("MEMBERSHIP_NOT_FOUND", "Organization membership was not found."),
                    cancellationToken);
            }
            if (request.ActorId != request.UserId && !OrganizationRolePermissions.GetPermissionsForRole(actorMembership.Role).Contains(Permissions.OrganizationMembersRemove))
                return await RollbackAsync(
                    ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You cannot remove this organization member."),
                    cancellationToken);
            if (request.ActorId != request.UserId && !CanManage(actorMembership, target, OrganizationRole.OrganizationMember))
                return await RollbackAsync(
                    ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You cannot remove this organization member."),
                    cancellationToken);
            if (target.Role == OrganizationRole.OrganizationOwner &&
                target.Status == MembershipStatus.Active &&
                await _members.GetActiveOwnerCountAsync(request.OrganizationId, cancellationToken) <= 1)
            {
                return await RollbackAsync(
                    ApplicationError.Conflict("LAST_OWNER_REQUIRED", "At least one active organization owner is required."),
                    cancellationToken);
            }

            var soleOwnerProjectIds = await _projectMembers.GetSoleOwnerProjectIdsAsync(request.OrganizationId, request.UserId, cancellationToken);
            if (soleOwnerProjectIds.Count > 0)
            {
                return await RollbackAsync(
                    ApplicationError.Conflict("LAST_OWNER_REQUIRED", "The member is the last active owner of one or more projects."),
                    cancellationToken);
            }

            var now = _clock.UtcNow;
            target.Change(target.Role, MembershipStatus.Removed, request.ActorId, target.Version, now);
            await _projectMembers.RemoveAllForOrganizationUserAsync(request.OrganizationId, request.UserId, now, cancellationToken);
            
            await _auditWriter.WriteAuditAsync(
                request.ActorId, null, "organization.member_removed", "organization-member", target.Id, "succeeded",
                new { organizationId = request.OrganizationId, userId = request.UserId, target.Version }, cancellationToken);
                
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
}
