using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Common.Text;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Organizations.Queries.ListOrganizationInvitations;

public sealed class ListOrganizationInvitationsQueryHandler : OrganizationQueryHandlerBase, IRequestHandler<ListOrganizationInvitationsQuery, Result<PagedResult<OrganizationInvitationDto>>>
{
    private readonly IUserInviteRepository _invitations;

    public ListOrganizationInvitationsQueryHandler(
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members,
        IUserInviteRepository invitations) : base(organizations, members)
    {
        _invitations = invitations;
    }

    public async Task<Result<PagedResult<OrganizationInvitationDto>>> Handle(ListOrganizationInvitationsQuery request, CancellationToken cancellationToken)
    {
        if (request.Page <= 0 || request.PageSize <= 0 || request.PageSize > 100)
            return ApplicationError.Validation("VALIDATION_ERROR", "page must be positive and pageSize must be between 1 and 100.");

        var access = await GetActiveAccessAsync(request.ActorId, request.OrganizationId, Permissions.OrganizationMembersInvite, cancellationToken);
        if (access.Error is not null) return access.Error;

        if (!string.IsNullOrWhiteSpace(request.Status) && !EnumText.TryParseDefined<InviteStatus>(request.Status, out _))
            return ApplicationError.Validation("VALIDATION_ERROR", "Invitation status is invalid.");

        var invitations = await _invitations.GetForOrganizationAsync(request.OrganizationId, request.Page, request.PageSize, request.Status, request.Email, cancellationToken);
        return new PagedResult<OrganizationInvitationDto>(
            invitations.Items.Select(OrganizationInvitationDto.From).ToList(),
            invitations.Page,
            invitations.PageSize,
            invitations.TotalItems);
    }
}
