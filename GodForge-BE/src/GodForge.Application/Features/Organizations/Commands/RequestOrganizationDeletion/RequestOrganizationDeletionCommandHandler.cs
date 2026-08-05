using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Domain.Entities.Core;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.RequestOrganizationDeletion;

public sealed class RequestOrganizationDeletionCommandHandler : OrganizationCommandHandlerBase, IRequestHandler<RequestOrganizationDeletionCommand, Result<DeletionAcceptedDto>>
{
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;

    public RequestOrganizationDeletionCommandHandler(
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members,
        IProjectMemberRepository projectMembers,
        IUnitOfWork unitOfWork,
        IAuditWriter auditWriter,
        IClock clock) : base(organizations, members, projectMembers, unitOfWork)
    {
        _auditWriter = auditWriter;
        _clock = clock;
    }

    public async Task<Result<DeletionAcceptedDto>> Handle(RequestOrganizationDeletionCommand request, CancellationToken cancellationToken)
    {
        var access = await GetActiveAccessAsync(request.ActorId, request.OrganizationId, Permissions.OrganizationsDelete, cancellationToken);
        if (access.Error is not null) return access.Error;
        
        var organization = access.Organization!;
        if (!string.Equals(organization.Slug, request.ConfirmationSlug?.Trim(), StringComparison.Ordinal))
            return ApplicationError.Validation("VALIDATION_ERROR", "confirmationSlug does not match the organization slug.");
        if (organization.Version != request.Version)
            return ApplicationError.Conflict("CONCURRENCY_CONFLICT", "Organization version is stale.");
            
        organization.MarkDeleting(request.Version, _clock.UtcNow);
        
        await _auditWriter.WriteAuditAsync(
            request.ActorId, null, "organization.deletion_requested", "organization", organization.Id, "succeeded",
            new { organization.Slug, organization.Version }, cancellationToken);
            
        var save = await SaveAsync(cancellationToken);
        if (save is not null) return save;
        
        return new DeletionAcceptedDto(organization.Id, "deleting", organization.Version);
    }
}
