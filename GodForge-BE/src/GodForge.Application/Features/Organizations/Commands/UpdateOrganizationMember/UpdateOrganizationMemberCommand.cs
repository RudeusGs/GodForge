using GodForge.Application.Common.Models;
using GodForge.Application.Features.Organizations.DTOs;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.UpdateOrganizationMember;

public sealed record UpdateOrganizationMemberCommand(
    Guid ActorId,
    Guid OrganizationId,
    Guid UserId,
    string Role,
    string Status,
    long Version) : IRequest<Result<OrganizationMemberDto>>;
