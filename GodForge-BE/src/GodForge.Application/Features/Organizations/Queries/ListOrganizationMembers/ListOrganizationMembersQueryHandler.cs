using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Common.Text;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Organizations.Queries.ListOrganizationMembers;

public sealed class ListOrganizationMembersQueryHandler : OrganizationQueryHandlerBase, IRequestHandler<ListOrganizationMembersQuery, Result<PagedResult<OrganizationMemberDto>>>
{
    private readonly IUserRepository _users;

    public ListOrganizationMembersQueryHandler(
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members,
        IUserRepository users) : base(organizations, members)
    {
        _users = users;
    }

    public async Task<Result<PagedResult<OrganizationMemberDto>>> Handle(ListOrganizationMembersQuery request, CancellationToken cancellationToken)
    {
        if (request.Page <= 0 || request.PageSize <= 0 || request.PageSize > 100)
            return ApplicationError.Validation("VALIDATION_ERROR", "page must be positive and pageSize must be between 1 and 100.");

        var access = await GetActiveAccessAsync(request.ActorId, request.OrganizationId, Permissions.OrganizationMembersRead, cancellationToken);
        if (access.Error is not null) return access.Error;

        if (!string.IsNullOrWhiteSpace(request.Role) &&
            !EnumText.TryParseDefined<OrganizationRole>(request.Role, out _))
        {
            return ApplicationError.Validation("VALIDATION_ERROR", "Role is invalid.");
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            !EnumText.TryParseDefined<MembershipStatus>(request.Status, out _))
        {
            return ApplicationError.Validation("VALIDATION_ERROR", "Status is invalid.");
        }

        var memberships = await _members.GetForOrganizationAsync(request.OrganizationId, request.Page, request.PageSize, request.Role, request.Status, request.Search, cancellationToken);
        var userIds = memberships.Items.Select(m => m.UserId).ToArray();
        var users = await _users.GetByIdsAsync(userIds, cancellationToken);
        var userById = users.ToDictionary(u => u.Id);

        var items = memberships.Items
            .Where(m => userById.ContainsKey(m.UserId))
            .Select(m => OrganizationMemberDto.From(m, userById[m.UserId]))
            .ToList();

        return new PagedResult<OrganizationMemberDto>(items, memberships.Page, memberships.PageSize, memberships.TotalItems);
    }
}
