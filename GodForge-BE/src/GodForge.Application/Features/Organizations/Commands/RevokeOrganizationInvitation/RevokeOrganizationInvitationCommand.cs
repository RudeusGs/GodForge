using GodForge.Application.Common.Models;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.RevokeOrganizationInvitation;

public sealed record RevokeOrganizationInvitationCommand(
    Guid ActorId,
    Guid OrganizationId,
    Guid InvitationId) : IRequest<Result>;
