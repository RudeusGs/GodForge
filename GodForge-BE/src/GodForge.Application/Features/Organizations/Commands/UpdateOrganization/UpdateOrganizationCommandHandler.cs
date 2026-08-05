using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Domain.Entities.Core;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.UpdateOrganization;

public sealed class UpdateOrganizationCommandHandler : OrganizationCommandHandlerBase, IRequestHandler<UpdateOrganizationCommand, Result<OrganizationDto>>
{
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;

    public UpdateOrganizationCommandHandler(
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

    private static bool ValidSlug(string? value) => !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 80 && System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-z0-9]+(?:-[a-z0-9]+)*$");

    public async Task<Result<OrganizationDto>> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var access = await GetActiveAccessAsync(request.ActorId, request.OrganizationId, Permissions.OrganizationsUpdate, cancellationToken);
        if (access.Error is not null) return access.Error;
        
        var organization = access.Organization!;
        var nextName = string.IsNullOrWhiteSpace(request.Name) ? organization.Name : request.Name.Trim();
        var nextSlug = string.IsNullOrWhiteSpace(request.Slug) ? organization.Slug : request.Slug.Trim();
        
        if (nextName.Length > 160 || !ValidSlug(nextSlug))
            return ApplicationError.Validation("VALIDATION_ERROR", "Organization name or slug is invalid.");
        if (nextSlug != organization.Slug && OrganizationSlugPolicy.IsReserved(nextSlug))
            return ApplicationError.Validation("ORGANIZATION_SLUG_RESERVED", "Organization slug is reserved.");
            
        var now = _clock.UtcNow;
        organization.Update(nextName, nextSlug, request.Version, now);
        
        await _auditWriter.WriteAuditAsync(
            request.ActorId, null, "organization.updated", "organization", organization.Id, "succeeded",
            new { organization.Name, organization.Slug, organization.Version }, cancellationToken);
            
        var save = await SaveAsync(cancellationToken);
        if (save is not null) return save;
        
        return OrganizationDto.From(organization, access.Membership!);
    }
}
