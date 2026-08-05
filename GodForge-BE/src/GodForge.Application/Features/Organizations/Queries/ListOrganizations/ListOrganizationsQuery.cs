using GodForge.Application.Common.Models;
using GodForge.Application.Features.Organizations.DTOs;
using MediatR;

namespace GodForge.Application.Features.Organizations.Queries.ListOrganizations;

public sealed record ListOrganizationsQuery(
    Guid ActorId,
    int Page,
    int PageSize,
    string? Status) : IRequest<Result<PagedResult<OrganizationDto>>>;
