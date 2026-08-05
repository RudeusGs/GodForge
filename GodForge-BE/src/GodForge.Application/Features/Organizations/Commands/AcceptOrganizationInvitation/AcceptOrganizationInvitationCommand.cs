using GodForge.Application.Common.Models;
using GodForge.Application.Features.Organizations.DTOs;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.AcceptOrganizationInvitation;

public sealed record AcceptOrganizationInvitationCommand(
    Guid ActorId,
    string Token) : IRequest<Result<OrganizationInvitationAcceptanceDto>>;
