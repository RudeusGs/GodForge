using System.Net.Mail;
using System.Security.Cryptography;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Common.Text;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Organizations.Commands.CreateOrganizationInvitation;

public sealed class CreateOrganizationInvitationCommandHandler : OrganizationCommandHandlerBase, IRequestHandler<CreateOrganizationInvitationCommand, Result<OrganizationInvitationDto>>
{
    private readonly IUserInviteRepository _invitations;
    private readonly ISecretHashService _secretHash;
    private readonly IFrontendUrlBuilder _frontendUrls;
    private readonly IEmailOutbox _emailOutbox;
    private readonly IAuditWriter _auditWriter;
    private readonly IM1QuotaPolicy _quotaPolicy;
    private readonly IClock _clock;

    public CreateOrganizationInvitationCommandHandler(
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members,
        IProjectMemberRepository projectMembers,
        IUnitOfWork unitOfWork,
        IUserInviteRepository invitations,
        ISecretHashService secretHash,
        IFrontendUrlBuilder frontendUrls,
        IEmailOutbox emailOutbox,
        IAuditWriter auditWriter,
        IM1QuotaPolicy quotaPolicy,
        IClock clock) : base(organizations, members, projectMembers, unitOfWork)
    {
        _invitations = invitations;
        _secretHash = secretHash;
        _frontendUrls = frontendUrls;
        _emailOutbox = emailOutbox;
        _auditWriter = auditWriter;
        _quotaPolicy = quotaPolicy;
        _clock = clock;
    }

    public async Task<Result<OrganizationInvitationDto>> Handle(CreateOrganizationInvitationCommand request, CancellationToken cancellationToken)
    {
        if (!EnumText.TryParseDefined<OrganizationRole>(request.Role, out var invitationRole) || invitationRole == OrganizationRole.OrganizationOwner)
            return ApplicationError.Validation("VALIDATION_ERROR", "Invitation role is invalid.");
        if (!TryNormalizeEmail(request.Email, out var email, out var normalizedEmail))
            return ApplicationError.Validation("VALIDATION_ERROR", "A valid email is required.");

        await BeginMembershipMutationAsync("organization-invitations", request.OrganizationId, cancellationToken);
        try
        {
            var access = await GetActiveAccessAsync(
                request.ActorId,
                request.OrganizationId,
                Permissions.OrganizationMembersInvite,
                cancellationToken);
            if (access.Error is not null)
                return await RollbackAsync<OrganizationInvitationDto>(access.Error, cancellationToken);
            if (access.Membership!.Role != OrganizationRole.OrganizationOwner && invitationRole == OrganizationRole.OrganizationAdmin)
                return await RollbackAsync<OrganizationInvitationDto>(
                    ApplicationError.Forbidden("SECURITY_FORBIDDEN", "Only an organization owner may invite an administrator."),
                    cancellationToken);

            var now = _clock.UtcNow;
            var existing = await _invitations.GetPendingAsync(request.OrganizationId, normalizedEmail, cancellationToken);
            if (existing is null &&
                await _invitations.CountPendingAsync(request.OrganizationId, cancellationToken) >= _quotaPolicy.MaxPendingInvitationsPerOrganization)
            {
                return await RollbackAsync<OrganizationInvitationDto>(
                    ApplicationError.TooManyRequests(
                        "ORGANIZATION_INVITATION_QUOTA_EXCEEDED",
                        "The pending organization invitation quota has been reached.",
                        new { limit = _quotaPolicy.MaxPendingInvitationsPerOrganization }),
                    cancellationToken);
            }

            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            var tokenHash = _secretHash.Hash(rawToken);
            UserInvite invitation;

            if (existing is null)
            {
                invitation = UserInvite.Create(
                    request.OrganizationId,
                    email,
                    invitationRole,
                    tokenHash,
                    request.ActorId,
                    now.AddDays(7),
                    now);
                await _invitations.AddAsync(invitation, cancellationToken);
            }
            else
            {
                invitation = existing;
                invitation.Replace(invitationRole, tokenHash, request.ActorId, now.AddDays(7), now);
            }

            var url = _frontendUrls.BuildOrganizationInvitationUrl(rawToken);
            await _emailOutbox.EnqueueAsync(
                email,
                "GodForge organization invitation",
                $"<p>You were invited to join an organization in GodForge.</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(url)}\">Accept invitation</a></p>",
                Guid.NewGuid().ToString("N"),
                cancellationToken);

            await _auditWriter.WriteAuditAsync(
                request.ActorId,
                null,
                "organization.invitation_created",
                "organization-invitation",
                invitation.Id,
                "succeeded",
                new
                {
                    organizationId = request.OrganizationId,
                    invitation.NormalizedEmail,
                    Role = invitation.Role.ToString(),
                    invitation.ExpiresAt
                },
                cancellationToken);

            var save = await SaveAsync(cancellationToken);
            if (save is not null)
                return await RollbackAsync<OrganizationInvitationDto>(save, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return OrganizationInvitationDto.From(invitation);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
    }

    private static bool TryNormalizeEmail(string? value, out string email, out string normalizedEmail)
    {
        email = string.Empty;
        normalizedEmail = string.Empty;
        if (string.IsNullOrWhiteSpace(value) || !MailAddress.TryCreate(value.Trim(), out var address))
            return false;

        email = address!.Address;
        if (!string.Equals(email, value.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        normalizedEmail = User.NormalizeEmail(email);
        return true;
    }
}
