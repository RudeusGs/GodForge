using GodForge.Application.Common.Idempotency;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.CreateOrganization;

public sealed class CreateOrganizationCommandHandler : OrganizationCommandHandlerBase, IRequestHandler<CreateOrganizationCommand, Result<OrganizationDto>>
{
    private readonly IUserRepository _users;
    private readonly IIdempotencyRepository _idempotency;
    private readonly IAuditWriter _auditWriter;
    private readonly IM1QuotaPolicy _quotaPolicy;
    private readonly IClock _clock;

    public CreateOrganizationCommandHandler(
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members,
        IProjectMemberRepository projectMembers,
        IUnitOfWork unitOfWork,
        IUserRepository users,
        IIdempotencyRepository idempotency,
        IAuditWriter auditWriter,
        IM1QuotaPolicy quotaPolicy,
        IClock clock) : base(organizations, members, projectMembers, unitOfWork)
    {
        _users = users;
        _idempotency = idempotency;
        _auditWriter = auditWriter;
        _quotaPolicy = quotaPolicy;
        _clock = clock;
    }

    public async Task<Result<OrganizationDto>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        if (!Organization.IsValidName(request.Name) || !Organization.IsValidSlug(request.Slug))
            return ApplicationError.Validation("VALIDATION_ERROR", "Organization name or slug is invalid.");
        if (OrganizationSlugPolicy.IsReserved(request.Slug))
            return ApplicationError.Validation("ORGANIZATION_SLUG_RESERVED", "Organization slug is reserved.");

        var idempotencyError = IdempotencyRequest.Normalize(request.IdempotencyKey, out var normalizedIdempotencyKey);
        if (idempotencyError is not null)
            return idempotencyError;

        var requestHash = IdempotencyRequest.Hash($"{request.Name.Trim()}\n{request.Slug.Trim()}");
        var actor = await _users.GetByIdAsync(request.ActorId, cancellationToken);
        if (actor is null || actor.Status != UserStatus.Active || actor.EmailVerifiedAt is null)
            return ApplicationError.Forbidden("SECURITY_FORBIDDEN", "A verified active account is required.");

        if (normalizedIdempotencyKey is not null)
        {
            var existingResult = await GetExistingCreateResultAsync(
                request.ActorId,
                normalizedIdempotencyKey,
                requestHash,
                cancellationToken);
            if (existingResult is not null)
                return existingResult;
        }

        await BeginMembershipMutationAsync("user-organization-catalog", request.ActorId, cancellationToken);
        try
        {
            actor = await _users.GetByIdAsync(request.ActorId, cancellationToken);
            if (actor is null || actor.Status != UserStatus.Active || actor.EmailVerifiedAt is null)
                return await RollbackAsync<OrganizationDto>(
                    ApplicationError.Forbidden("SECURITY_FORBIDDEN", "A verified active account is required."),
                    cancellationToken);

            if (normalizedIdempotencyKey is not null)
            {
                var existingResult = await GetExistingCreateResultAsync(
                    request.ActorId,
                    normalizedIdempotencyKey,
                    requestHash,
                    cancellationToken);
                if (existingResult is not null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    _unitOfWork.ClearTrackedChanges();
                    return existingResult;
                }
            }

            if (await _organizations.CountCreatedByAsync(request.ActorId, cancellationToken) >= _quotaPolicy.MaxOrganizationsPerUser)
                return await RollbackAsync<OrganizationDto>(
                    ApplicationError.TooManyRequests(
                        "ORGANIZATION_QUOTA_EXCEEDED",
                        "The account organization quota has been reached.",
                        new { limit = _quotaPolicy.MaxOrganizationsPerUser }),
                    cancellationToken);
            if (await _organizations.SlugExistsAsync(request.Slug, cancellationToken: cancellationToken))
                return await RollbackAsync<OrganizationDto>(
                    ApplicationError.Conflict("ORGANIZATION_SLUG_EXISTS", "Organization slug already exists."),
                    cancellationToken);

            var now = _clock.UtcNow;
            var organization = Organization.Create(request.Name, request.Slug, request.ActorId, now);
            var membership = OrganizationMember.CreateOwner(organization.Id, request.ActorId, now);

            await _organizations.AddAsync(organization, cancellationToken);
            await _members.AddAsync(membership, cancellationToken);

            if (normalizedIdempotencyKey is not null)
            {
                await _idempotency.AddAsync(IdempotencyRecord.Create(
                    request.ActorId,
                    "organization.create",
                    normalizedIdempotencyKey,
                    requestHash,
                    "organization",
                    organization.Id,
                    now), cancellationToken);
            }

            await _auditWriter.WriteAuditAsync(
                request.ActorId,
                null,
                "organization.created",
                "organization",
                organization.Id,
                "succeeded",
                new { organization.Name, organization.Slug },
                cancellationToken);

            var save = await SaveAsync(cancellationToken);
            if (save is not null)
                return await RollbackAsync<OrganizationDto>(save, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return OrganizationDto.From(organization, membership);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
    }
    private async Task<Result<OrganizationDto>?> GetExistingCreateResultAsync(
        Guid actorId,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var existingRecord = await _idempotency.GetAsync(
            actorId,
            "organization.create",
            idempotencyKey,
            cancellationToken);
        if (existingRecord is null)
            return null;

        if (!string.Equals(existingRecord.RequestHash, requestHash, StringComparison.Ordinal))
            return ApplicationError.Conflict(
                "IDEMPOTENCY_KEY_REUSED",
                "The idempotency key was already used with a different request.");

        var existingOrganization = await _organizations.GetByIdAsync(existingRecord.ResourceId, cancellationToken);
        var existingMembership = await _members.GetAsync(existingRecord.ResourceId, actorId, cancellationToken);
        if (existingOrganization is not null && existingMembership is not null)
            return OrganizationDto.From(existingOrganization, existingMembership);

        return ApplicationError.Conflict(
            "IDEMPOTENCY_RESOURCE_UNAVAILABLE",
            "The resource recorded for this idempotency key is unavailable.");
    }

}
