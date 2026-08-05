using GodForge.Application.Common.Models;
using GodForge.Application.Features.Organizations.DTOs;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.RemoveOrganizationMember;

public sealed record RemoveOrganizationMemberCommand(
    Guid ActorId,
    Guid OrganizationId,
    Guid UserId) : IRequest<Result>;
