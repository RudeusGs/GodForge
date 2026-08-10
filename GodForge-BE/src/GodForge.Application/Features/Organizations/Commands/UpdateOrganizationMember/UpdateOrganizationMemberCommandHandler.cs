using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Common.Text;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.UpdateOrganizationMember;

public sealed class UpdateOrganizationMemberCommandHandler : OrganizationCommandHandlerBase, IRequestHandler<UpdateOrganizationMemberCommand, Result<OrganizationMemberDto>>
{
    private readonly IUserRepository _users;
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;

    public UpdateOrganizationMemberCommandHandler(
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members,
        IProjectMemberRepository projectMembers,
        IUnitOfWork unitOfWork,
        IUserRepository users,
        IAuditWriter auditWriter,
        IClock clock) : base(organizations, members, projectMembers, unitOfWork)
    {
        _users = users;
        _auditWriter = auditWriter;
        _clock = clock;
    }

    public async Task<Result<OrganizationMemberDto>> Handle(UpdateOrganizationMemberCommand request, CancellationToken cancellationToken)
    {
        if (!EnumText.TryParseDefined<OrganizationRole>(request.Role, out var newRole) ||
            !EnumText.TryParseDefined<MembershipStatus>(request.Status, out var newStatus) ||
            newStatus == MembershipStatus.Removed)
        {
            return ApplicationError.Validation("VALIDATION_ERROR", "Membership role or status is invalid.");
        }

        await BeginMembershipMutationAsync("organization-membership", request.OrganizationId, cancellationToken);
        try
        {
            var access = await GetActiveAccessAsync(request.ActorId, request.OrganizationId, Permissions.OrganizationMembersUpdateRole, cancellationToken);
            if (access.Error is not null)
                return await RollbackAsync<OrganizationMemberDto>(access.Error, cancellationToken);

            var target = await _members.GetAsync(request.OrganizationId, request.UserId, cancellationToken);
            var targetUser = await _users.GetByIdAsync(request.UserId, cancellationToken);
            if (target is null || targetUser is null)
                return await RollbackAsync<OrganizationMemberDto>(
                    ApplicationError.NotFound("MEMBERSHIP_NOT_FOUND", "Organization membership was not found."),
                    cancellationToken);
            if (target.Version != request.Version)
                return await RollbackAsync<OrganizationMemberDto>(
                    ApplicationError.Conflict("CONCURRENCY_CONFLICT", "Membership version is stale."),
                    cancellationToken);
            if (!CanManage(access.Membership!, target, newRole))
                return await RollbackAsync<OrganizationMemberDto>(
                    ApplicationError.Forbidden("SECURITY_FORBIDDEN", "The requested membership change is outside the actor's grant boundary."),
                    cancellationToken);
            if (target.Role == OrganizationRole.OrganizationOwner &&
                (newRole != OrganizationRole.OrganizationOwner || newStatus != MembershipStatus.Active) &&
                await _members.GetActiveOwnerCountAsync(request.OrganizationId, cancellationToken) <= 1)
            {
                return await RollbackAsync<OrganizationMemberDto>(
                    ApplicationError.Conflict("LAST_OWNER_REQUIRED", "At least one active organization owner is required."),
                    cancellationToken);
            }

            IReadOnlyList<Guid> affectedProjectIds = Array.Empty<Guid>();
            if (newStatus == MembershipStatus.Suspended)
            {
                affectedProjectIds = await LockAffectedProjectsAsync(request.OrganizationId, request.UserId, cancellationToken);
                var soleOwnerProjectIds = await _projectMembers.GetSoleOwnerProjectIdsAsync(request.OrganizationId, request.UserId, cancellationToken);
                if (soleOwnerProjectIds.Count > 0)
                {
                    return await RollbackAsync<OrganizationMemberDto>(
                        ApplicationError.Conflict("LAST_OWNER_REQUIRED", "The member is the last active owner of one or more projects."),
                        cancellationToken);
                }
            }

            var now = _clock.UtcNow;
            target.Change(newRole, newStatus, request.ActorId, request.Version, now);

            if (newStatus == MembershipStatus.Suspended)
                await _projectMembers.SuspendAllForOrganizationUserAsync(request.OrganizationId, request.UserId, now, cancellationToken);

            await _auditWriter.WriteAuditAsync(
                request.ActorId, null, "organization.member_updated", "organization-member", target.Id, "succeeded",
                new { organizationId = request.OrganizationId, userId = request.UserId, Role = newRole.ToString(), Status = newStatus.ToString(), target.Version }, cancellationToken);

            var save = await SaveAsync(cancellationToken);
            if (save is not null)
                return await RollbackAsync<OrganizationMemberDto>(save, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return OrganizationMemberDto.From(target, targetUser);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
    }
}
