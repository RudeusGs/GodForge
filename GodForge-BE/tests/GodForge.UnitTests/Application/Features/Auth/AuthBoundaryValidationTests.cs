using GodForge.Application.Features.Auth.Commands.ForgotPassword;
using GodForge.Application.Features.Auth.Commands.Register;
using GodForge.Application.Features.Auth.Commands.ResetPassword;
using GodForge.Application.Features.Auth.Commands.SendOtp;
using GodForge.Domain.Entities.Identity;

namespace GodForge.UnitTests.Application.Features.Auth;

public sealed class AuthBoundaryValidationTests
{
    [Fact]
    public void RegisterValidator_EmailLongerThanDatabaseColumn_IsRejected()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand(
            CreateLongEmail(),
            "Test User",
            "ValidPassword1",
            "123456");

        var result = validator.Validate(command);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(command.Email));
    }

    [Fact]
    public void RegisterValidator_PasswordLongerThanLoginContract_IsRejected()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand(
            "user@example.com",
            "Test User",
            "A1" + new string('a', 255),
            "123456");

        var result = validator.Validate(command);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(command.Password));
    }

    [Fact]
    public void ResetPasswordValidator_PasswordLongerThanLoginContract_IsRejected()
    {
        var validator = new ResetPasswordCommandValidator();
        var command = new ResetPasswordCommand(
            "user@example.com",
            "token",
            "A1" + new string('a', 255));

        var result = validator.Validate(command);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(command.NewPassword));
    }

    [Fact]
    public void ForgotPasswordAndOtpValidators_EmailLongerThanDatabaseColumn_IsRejected()
    {
        var email = CreateLongEmail();

        var forgotResult = new ForgotPasswordCommandValidator()
            .Validate(new ForgotPasswordCommand(email, "correlation"));
        var otpResult = new SendOtpCommandValidator()
            .Validate(new SendOtpCommand(email, "correlation"));

        Assert.Contains(forgotResult.Errors, error => error.PropertyName == "Email");
        Assert.Contains(otpResult.Errors, error => error.PropertyName == "Email");
    }

    private static string CreateLongEmail()
        => new string('a', User.MaxEmailLength - "@example.com".Length + 1) + "@example.com";
}
