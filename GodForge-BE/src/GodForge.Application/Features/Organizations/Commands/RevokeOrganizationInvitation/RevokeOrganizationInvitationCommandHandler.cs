using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.RevokeOrganizationInvitation;

public sealed class RevokeOrganizationInvitationCommandHandler : OrganizationCommandHandlerBase, IRequestHandler<RevokeOrganizationInvitationCommand, Result>
{
    private readonly IUserInviteRepository _invitations;
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;

    public RevokeOrganizationInvitationCommandHandler(
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members,
        IProjectMemberRepository projectMembers,
        IUnitOfWork unitOfWork,
        IUserInviteRepository invitations,
        IAuditWriter auditWriter,
        IClock clock) : base(organizations, members, projectMembers, unitOfWork)
    {
        _invitations = invitations;
        _auditWriter = auditWriter;
        _clock = clock;
    }

    public async Task<Result> Handle(RevokeOrganizationInvitationCommand request, CancellationToken cancellationToken)
    {
        var access = await GetActiveAccessAsync(request.ActorId, request.OrganizationId, Permissions.OrganizationMembersInvite, cancellationToken);
        if (access.Error is not null) return access.Error;

        var invitation = await _invitations.GetByIdAsync(request.OrganizationId, request.InvitationId, cancellationToken);
        if (invitation is null)
            return ApplicationError.NotFound("RESOURCE_NOT_FOUND", "Invitation was not found.");

        invitation.Revoke(_clock.UtcNow);

        await _auditWriter.WriteAuditAsync(
            request.ActorId, null, "organization.invitation_revoked", "organization-invitation", invitation.Id, "succeeded",
            new { organizationId = request.OrganizationId, invitation.NormalizedEmail }, cancellationToken);

        var save = await SaveAsync(cancellationToken);
        return save is null ? Result.Success() : Result.Failure(save);
    }
}
