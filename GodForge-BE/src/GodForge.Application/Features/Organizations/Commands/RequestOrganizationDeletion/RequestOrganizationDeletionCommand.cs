using GodForge.Application.Common.Models;
using GodForge.Application.Features.Organizations.DTOs;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.RequestOrganizationDeletion;

public sealed record RequestOrganizationDeletionCommand(
    Guid ActorId,
    Guid OrganizationId,
    long Version,
    string ConfirmationSlug) : IRequest<Result<DeletionAcceptedDto>>;
