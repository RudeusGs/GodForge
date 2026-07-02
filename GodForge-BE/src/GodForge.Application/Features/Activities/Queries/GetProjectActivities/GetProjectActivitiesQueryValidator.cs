using FluentValidation;

namespace GodForge.Application.Features.Activities.Queries.GetProjectActivities;

public class GetProjectActivitiesQueryValidator : AbstractValidator<GetProjectActivitiesQuery>
{
    public GetProjectActivitiesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
