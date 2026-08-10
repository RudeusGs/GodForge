namespace GodForge.Api.Contracts.Auth;

public sealed record LoginRequest(string Email, string Password, string? DeviceName);
public sealed record RegisterRequest(string Email, string DisplayName, string Password, string Otp);
public sealed record SendRegisterOtpRequest(string Email);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
