using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Organizations.Queries.GetOrganization;

public sealed class GetOrganizationQueryHandler : OrganizationQueryHandlerBase, IRequestHandler<GetOrganizationQuery, Result<OrganizationDto>>
{
    public GetOrganizationQueryHandler(
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members) : base(organizations, members)
    {
    }

    public async Task<Result<OrganizationDto>> Handle(GetOrganizationQuery request, CancellationToken cancellationToken)
    {
        var access = await GetActiveAccessAsync(request.ActorId, request.OrganizationId, Permissions.OrganizationsRead, cancellationToken);
        if (access.Error is not null) return access.Error;

        return OrganizationDto.From(access.Organization!, access.Membership!);
    }
}
