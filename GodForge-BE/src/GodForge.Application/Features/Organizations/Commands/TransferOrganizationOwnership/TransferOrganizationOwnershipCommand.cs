using GodForge.Application.Common.Models;
using GodForge.Application.Features.Organizations.DTOs;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.TransferOrganizationOwnership;

public sealed record TransferOrganizationOwnershipCommand(
    Guid ActorId,
    Guid OrganizationId,
    Guid NewOwnerUserId,
    string RetainCurrentOwnerAs,
    long Version) : IRequest<Result<OrganizationOwnershipTransferDto>>;
