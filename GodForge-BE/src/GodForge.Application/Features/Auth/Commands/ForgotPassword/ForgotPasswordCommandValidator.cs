using FluentValidation;
using GodForge.Domain.Entities.Identity;

namespace GodForge.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(User.MaxEmailLength).WithMessage($"Email must not exceed {User.MaxEmailLength} characters.");
    }
}
