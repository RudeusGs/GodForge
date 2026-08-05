using FluentValidation;

namespace GodForge.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(320).EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
        RuleFor(x => x.DeviceName).MaximumLength(200);
        RuleFor(x => x.UserAgent).MaximumLength(500);
        RuleFor(x => x.IpAddress).MaximumLength(45);
    }
}
