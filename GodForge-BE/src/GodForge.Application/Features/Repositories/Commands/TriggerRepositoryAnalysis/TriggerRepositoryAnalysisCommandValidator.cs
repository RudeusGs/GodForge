using FluentValidation;
using GodForge.Application.Common.Models.Analysis;

namespace GodForge.Application.Features.Repositories.Commands.TriggerRepositoryAnalysis;

public sealed class TriggerRepositoryAnalysisCommandValidator : AbstractValidator<TriggerRepositoryAnalysisCommand>
{
    public TriggerRepositoryAnalysisCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Branch).MaximumLength(150);
        RuleFor(x => x.AnalysisProfile)
            .NotEmpty()
            .Must(AiAdvisoryModes.IsSupported)
            .WithMessage("AnalysisProfile must select a supported AI advisory mode, not a deterministic analysis profile key.");
    }
}
