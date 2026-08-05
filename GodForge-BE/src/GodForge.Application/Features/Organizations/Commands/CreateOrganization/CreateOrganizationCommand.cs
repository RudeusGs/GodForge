using GodForge.Application.Common.Models;
using GodForge.Application.Features.Organizations.DTOs;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.CreateOrganization;

public sealed record CreateOrganizationCommand(
    Guid ActorId,
    string Name,
    string Slug,
    string? IdempotencyKey) : IRequest<Result<OrganizationDto>>;
