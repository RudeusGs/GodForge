using FluentValidation;

namespace GodForge.Application.Features.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.");

        RuleFor(v => v.DisplayName)
            .NotEmpty().WithMessage("DisplayName is required.")
            .MaximumLength(100).WithMessage("DisplayName must not exceed 100 characters.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");

        RuleFor(v => v.Otp)
            .NotEmpty().WithMessage("OTP verification code is required.")
            .Length(6).WithMessage("OTP must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("OTP must contain only digits.");
    }
}

