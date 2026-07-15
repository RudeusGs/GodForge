using FluentValidation;
using GodForge.Domain.Enums;

namespace GodForge.Application.Features.Projects.Commands.CreateProject;

public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Visibility).IsEnumName(typeof(ProjectVisibility), caseSensitive: false)
            .WithMessage("Visibility must be a valid enum value (Private or Internal).");
    }
}

