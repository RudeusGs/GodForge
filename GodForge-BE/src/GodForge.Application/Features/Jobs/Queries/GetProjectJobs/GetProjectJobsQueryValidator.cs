using FluentValidation;

namespace GodForge.Application.Features.Jobs.Queries.GetProjectJobs;

public class GetProjectJobsQueryValidator : AbstractValidator<GetProjectJobsQuery>
{
    public GetProjectJobsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
