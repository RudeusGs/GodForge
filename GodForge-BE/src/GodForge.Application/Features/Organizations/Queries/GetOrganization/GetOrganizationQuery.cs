using GodForge.Application.Common.Models;
using GodForge.Application.Features.Organizations.DTOs;
using MediatR;

namespace GodForge.Application.Features.Organizations.Queries.GetOrganization;

public sealed record GetOrganizationQuery(
    Guid ActorId,
    Guid OrganizationId) : IRequest<Result<OrganizationDto>>;
