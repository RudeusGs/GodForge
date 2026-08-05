using GodForge.Application.Common.Models;
using GodForge.Application.Features.Organizations.DTOs;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.CreateOrganizationInvitation;

public sealed record CreateOrganizationInvitationCommand(
    Guid ActorId,
    Guid OrganizationId,
    string Email,
    string Role) : IRequest<Result<OrganizationInvitationDto>>;
