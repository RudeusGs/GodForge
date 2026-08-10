using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Common.Text;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.TransferOrganizationOwnership;

public sealed class TransferOrganizationOwnershipCommandHandler : OrganizationCommandHandlerBase, IRequestHandler<TransferOrganizationOwnershipCommand, Result<OrganizationOwnershipTransferDto>>
{
    private readonly IUserRepository _users;
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;

    public TransferOrganizationOwnershipCommandHandler(
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

    public async Task<Result<OrganizationOwnershipTransferDto>> Handle(TransferOrganizationOwnershipCommand request, CancellationToken cancellationToken)
    {
        if (request.ActorId == request.NewOwnerUserId)
            return ApplicationError.Validation("VALIDATION_ERROR", "The target user is already the current owner.");
        if (!EnumText.TryParseDefined<OrganizationRole>(request.RetainCurrentOwnerAs, out var retainedRole) || retainedRole == OrganizationRole.OrganizationOwner)
            return ApplicationError.Validation("VALIDATION_ERROR", "retainCurrentOwnerAs must be organizationAdmin or organizationMember.");

        await BeginMembershipMutationAsync("organization-membership", request.OrganizationId, cancellationToken);
        try
        {
            var access = await GetActiveAccessAsync(request.ActorId, request.OrganizationId, Permissions.OrganizationsTransferOwnership, cancellationToken);
            if (access.Error is not null)
                return await RollbackAsync<OrganizationOwnershipTransferDto>(access.Error, cancellationToken);
            if (access.Organization!.Version != request.Version)
                return await RollbackAsync<OrganizationOwnershipTransferDto>(
                    ApplicationError.Conflict("CONCURRENCY_CONFLICT", "Organization version is stale."),
                    cancellationToken);

            var target = await _members.GetAsync(request.OrganizationId, request.NewOwnerUserId, cancellationToken);
            var users = await _users.GetByIdsAsync(new[] { request.ActorId, request.NewOwnerUserId }, cancellationToken);
            var usersById = users.ToDictionary(user => user.Id);
            if (target is not { Status: MembershipStatus.Active } ||
                !usersById.TryGetValue(request.NewOwnerUserId, out var targetUser) ||
                !usersById.TryGetValue(request.ActorId, out var actorUser))
            {
                return await RollbackAsync<OrganizationOwnershipTransferDto>(
                    ApplicationError.NotFound("MEMBERSHIP_NOT_FOUND", "Target organization membership was not found."),
                    cancellationToken);
            }

            var now = _clock.UtcNow;
            var previousOwner = access.Membership!;
            target.Change(OrganizationRole.OrganizationOwner, MembershipStatus.Active, request.ActorId, target.Version, now);
            previousOwner.Change(retainedRole, MembershipStatus.Active, request.ActorId, previousOwner.Version, now);

            await _auditWriter.WriteAuditAsync(
                request.ActorId, null, "organization.ownership_transferred", "organization", request.OrganizationId, "succeeded",
                new { PreviousOwnerUserId = request.ActorId, NewOwnerUserId = request.NewOwnerUserId, RetainedRole = retainedRole.ToString() }, cancellationToken);

            var save = await SaveAsync(cancellationToken);
            if (save is not null)
                return await RollbackAsync<OrganizationOwnershipTransferDto>(save, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return new OrganizationOwnershipTransferDto(
                OrganizationMemberDto.From(previousOwner, actorUser),
                OrganizationMemberDto.From(target, targetUser));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
    }
}
