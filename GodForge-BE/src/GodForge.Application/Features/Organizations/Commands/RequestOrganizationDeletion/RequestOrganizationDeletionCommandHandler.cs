using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
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
        if (request.Version <= 0)
            return ApplicationError.Validation("VALIDATION_ERROR", "Organization version must be positive.");

        await BeginMembershipMutationAsync("organization-membership", request.OrganizationId, cancellationToken);
        try
        {
            var organization = await _organizations.GetByIdAsync(request.OrganizationId, cancellationToken);
            var membership = await _members.GetAsync(request.OrganizationId, request.ActorId, cancellationToken);
            if (organization is null || organization.Status == OrganizationStatus.Deleted || membership is not { Status: MembershipStatus.Active })
                return await RollbackAsync<DeletionAcceptedDto>(
                    ApplicationError.NotFound("ORGANIZATION_NOT_FOUND", "Organization was not found."),
                    cancellationToken);
            if (!OrganizationRolePermissions.GetPermissionsForRole(membership.Role).Contains(Permissions.OrganizationsDelete))
                return await RollbackAsync<DeletionAcceptedDto>(
                    ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You do not have permission for this organization operation."),
                    cancellationToken);
            if (!string.Equals(organization.Slug, request.ConfirmationSlug?.Trim(), StringComparison.Ordinal))
                return await RollbackAsync<DeletionAcceptedDto>(
                    ApplicationError.Validation("VALIDATION_ERROR", "confirmationSlug does not match the organization slug."),
                    cancellationToken);

            if (organization.Status == OrganizationStatus.Deleting)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _unitOfWork.ClearTrackedChanges();
                return new DeletionAcceptedDto(organization.Id, "deleting", organization.Version);
            }

            if (organization.Status != OrganizationStatus.Active)
                return await RollbackAsync<DeletionAcceptedDto>(
                    ApplicationError.Conflict("ORGANIZATION_NOT_ACTIVE", "Organization is not active."),
                    cancellationToken);
            if (organization.Version != request.Version)
                return await RollbackAsync<DeletionAcceptedDto>(
                    ApplicationError.Conflict("CONCURRENCY_CONFLICT", "Organization version is stale."),
                    cancellationToken);

            organization.MarkDeleting(request.Version, _clock.UtcNow);

            await _auditWriter.WriteAuditAsync(
                request.ActorId, null, "organization.deletion_requested", "organization", organization.Id, "succeeded",
                new { organization.Slug, organization.Version }, cancellationToken);

            var save = await SaveAsync(cancellationToken);
            if (save is not null)
                return await RollbackAsync<DeletionAcceptedDto>(save, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return new DeletionAcceptedDto(organization.Id, "deleting", organization.Version);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
    }
}
