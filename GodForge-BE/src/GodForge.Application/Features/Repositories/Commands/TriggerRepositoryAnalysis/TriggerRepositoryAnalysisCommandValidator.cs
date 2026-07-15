using FluentValidation;

namespace GodForge.Application.Features.Repositories.Commands.TriggerRepositoryAnalysis;

public sealed class TriggerRepositoryAnalysisCommandValidator : AbstractValidator<TriggerRepositoryAnalysisCommand>
{
    private static readonly string[] SupportedProfiles = { "health_overview", "commit_review", "architecture_review" };

    public TriggerRepositoryAnalysisCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Branch).MaximumLength(150);
        RuleFor(x => x.AnalysisProfile)
            .NotEmpty()
            .Must(profile => SupportedProfiles.Contains(profile, StringComparer.Ordinal))
            .WithMessage("AnalysisProfile is not supported.");
    }
}
