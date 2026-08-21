using GodForge.Application.Features.Organizations.Queries.ListOrganizationInvitations;
using GodForge.Application.Features.Organizations.Queries.ListOrganizationMembers;
using GodForge.Domain.Entities.Identity;

namespace GodForge.UnitTests.Application.Features.Organizations;

public sealed class OrganizationQueryValidationTests
{
    [Fact]
    public void ListOrganizationMembersQueryValidator_RejectsOversizedSearch()
    {
        var validator = new ListOrganizationMembersQueryValidator();
        var query = new ListOrganizationMembersQuery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            20,
            null,
            null,
            new string('x', 201));

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(query.Search));
    }

    [Fact]
    public void ListOrganizationInvitationsQueryValidator_RejectsOversizedEmailFilter()
    {
        var validator = new ListOrganizationInvitationsQueryValidator();
        var query = new ListOrganizationInvitationsQuery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            20,
            null,
            new string('x', User.MaxEmailLength + 1));

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(query.Email));
    }
}
