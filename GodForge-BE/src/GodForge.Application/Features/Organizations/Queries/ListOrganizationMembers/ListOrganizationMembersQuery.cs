using GodForge.Application.Common.Models;
using GodForge.Application.Features.Organizations.DTOs;
using MediatR;

namespace GodForge.Application.Features.Organizations.Queries.ListOrganizationMembers;

public sealed record ListOrganizationMembersQuery(
    Guid ActorId,
    Guid OrganizationId,
    int Page,
    int PageSize,
    string? Role,
    string? Status,
    string? Search) : IRequest<Result<PagedResult<OrganizationMemberDto>>>;
