using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Organizations.Queries.ListOrganizations;

public sealed class ListOrganizationsQueryHandler : IRequestHandler<ListOrganizationsQuery, Result<PagedResult<OrganizationDto>>>
{
    private readonly IOrganizationRepository _organizations;
    private readonly IOrganizationMemberRepository _members;

    public ListOrganizationsQueryHandler(
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members)
    {
        _organizations = organizations;
        _members = members;
    }

    public async Task<Result<PagedResult<OrganizationDto>>> Handle(ListOrganizationsQuery request, CancellationToken cancellationToken)
    {
        if (request.Page <= 0 || request.PageSize <= 0 || request.PageSize > 100)
            return ApplicationError.Validation("VALIDATION_ERROR", "page must be positive and pageSize must be between 1 and 100.");

        OrganizationStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<OrganizationStatus>(request.Status, true, out var value))
                return ApplicationError.Validation("VALIDATION_ERROR", "Organization status is invalid.");
            parsedStatus = value;
        }

        var organizations = await _organizations.GetForMemberAsync(request.ActorId, request.Page, request.PageSize, parsedStatus, cancellationToken);
        var organizationIds = organizations.Items.Select(organization => organization.Id).ToArray();
        var memberships = await _members.GetForOrganizationsAsync(organizationIds, request.ActorId, cancellationToken);
        var membershipByOrganization = memberships.ToDictionary(membership => membership.OrganizationId);
        var items = organizations.Items
            .Where(organization => membershipByOrganization.ContainsKey(organization.Id))
            .Select(organization => OrganizationDto.From(organization, membershipByOrganization[organization.Id]))
            .ToList();

        return new PagedResult<OrganizationDto>(items, organizations.Page, organizations.PageSize, organizations.TotalItems);
    }
}
