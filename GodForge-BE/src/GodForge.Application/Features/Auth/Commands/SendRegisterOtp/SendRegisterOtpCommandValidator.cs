using FluentValidation;

namespace GodForge.Application.Features.Auth.Commands.SendRegisterOtp;

public sealed class SendRegisterOtpCommandValidator : AbstractValidator<SendRegisterOtpCommand>
{
    public SendRegisterOtpCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.");
    }
}
