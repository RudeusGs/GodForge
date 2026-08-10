using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.AcceptOrganizationInvitation;

public sealed class AcceptOrganizationInvitationCommandHandler : OrganizationCommandHandlerBase, IRequestHandler<AcceptOrganizationInvitationCommand, Result<OrganizationInvitationAcceptanceDto>>
{
    private readonly IUserInviteRepository _invitations;
    private readonly IUserRepository _users;
    private readonly ISecretHashService _secretHash;
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;

    public AcceptOrganizationInvitationCommandHandler(
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members,
        IProjectMemberRepository projectMembers,
        IUnitOfWork unitOfWork,
        IUserInviteRepository invitations,
        IUserRepository users,
        ISecretHashService secretHash,
        IAuditWriter auditWriter,
        IClock clock) : base(organizations, members, projectMembers, unitOfWork)
    {
        _invitations = invitations;
        _users = users;
        _secretHash = secretHash;
        _auditWriter = auditWriter;
        _clock = clock;
    }

    public async Task<Result<OrganizationInvitationAcceptanceDto>> Handle(AcceptOrganizationInvitationCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return ApplicationError.Unauthorized("INVITE_INVALID_OR_EXPIRED", "Invitation is invalid or expired.");

        var invitation = await _invitations.GetByTokenHashAsync(_secretHash.Hash(request.Token), cancellationToken);
        var user = await _users.GetByIdAsync(request.ActorId, cancellationToken);
        var now = _clock.UtcNow;

        if (invitation is null || !invitation.IsActive(now) || user is null || user.EmailVerifiedAt is null || user.NormalizedEmail != invitation.NormalizedEmail)
            return ApplicationError.Unauthorized("INVITE_INVALID_OR_EXPIRED", "Invitation is invalid or expired.");

        var organization = await _organizations.GetByIdAsync(invitation.OrganizationId, cancellationToken);
        if (organization is null || organization.Status != OrganizationStatus.Active)
            return ApplicationError.Unauthorized("INVITE_INVALID_OR_EXPIRED", "Invitation is invalid or expired.");

        var membership = await _members.GetAsync(organization.Id, request.ActorId, cancellationToken);
        if (membership is null)
        {
            membership = OrganizationMember.Create(organization.Id, request.ActorId, invitation.Role, invitation.InvitedBy, now);
            await _members.AddAsync(membership, cancellationToken);
        }
        else if (membership.Status != MembershipStatus.Active)
        {
            membership.Change(invitation.Role, MembershipStatus.Active, invitation.InvitedBy, membership.Version, now);
        }

        invitation.Accept(now);

        await _auditWriter.WriteAuditAsync(
            request.ActorId, null, "organization.invitation_accepted", "organization-invitation", invitation.Id, "succeeded",
            new { organizationId = organization.Id, Role = membership.Role.ToString() }, cancellationToken);

        var save = await SaveAsync(cancellationToken);
        if (save is not null) return save;

        return new OrganizationInvitationAcceptanceDto(
            OrganizationDto.From(organization, membership),
            OrganizationMemberDto.From(membership, user));
    }
}
