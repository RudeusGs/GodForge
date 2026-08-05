using GodForge.Application.Common.Models;
using GodForge.Application.Features.Organizations.DTOs;
using MediatR;

namespace GodForge.Application.Features.Organizations.Queries.ListOrganizationInvitations;

public sealed record ListOrganizationInvitationsQuery(
    Guid ActorId,
    Guid OrganizationId,
    int Page,
    int PageSize,
    string? Status,
    string? Email) : IRequest<Result<PagedResult<OrganizationInvitationDto>>>;
