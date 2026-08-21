using FluentValidation;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Organizations.DTOs;
using MediatR;

namespace GodForge.Application.Features.Organizations.Queries.ListOrganizationMembers;

public sealed record ListOrganizationMembersQuery(
    Guid ActorId,
    Guid OrganizationId,
    int Page,
    int PageSize,
    string? Role,
    string? Status,
    string? Search) : IRequest<Result<PagedResult<OrganizationMemberDto>>>;

public sealed class ListOrganizationMembersQueryValidator : AbstractValidator<ListOrganizationMembersQuery>
{
    public ListOrganizationMembersQueryValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.OrganizationId).NotEmpty();
        RuleFor(request => request.Page).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Role).MaximumLength(30);
        RuleFor(request => request.Status).MaximumLength(30);
        RuleFor(request => request.Search).MaximumLength(200);
    }
}
