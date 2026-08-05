using GodForge.Application.Common.Models;
using GodForge.Application.Features.Organizations.DTOs;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.UpdateOrganization;

public sealed record UpdateOrganizationCommand(
    Guid ActorId,
    Guid OrganizationId,
    string? Name,
    string? Slug,
    long Version) : IRequest<Result<OrganizationDto>>;
