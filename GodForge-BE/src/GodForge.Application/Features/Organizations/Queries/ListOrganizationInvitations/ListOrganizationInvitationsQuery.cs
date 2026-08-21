using FluentValidation;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Domain.Entities.Identity;
using MediatR;

namespace GodForge.Application.Features.Organizations.Queries.ListOrganizationInvitations;

public sealed record ListOrganizationInvitationsQuery(
    Guid ActorId,
    Guid OrganizationId,
    int Page,
    int PageSize,
    string? Status,
    string? Email) : IRequest<Result<PagedResult<OrganizationInvitationDto>>>;

public sealed class ListOrganizationInvitationsQueryValidator : AbstractValidator<ListOrganizationInvitationsQuery>
{
    public ListOrganizationInvitationsQueryValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.OrganizationId).NotEmpty();
        RuleFor(request => request.Page).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Status).MaximumLength(30);
        RuleFor(request => request.Email).MaximumLength(User.MaxEmailLength);
    }
}
