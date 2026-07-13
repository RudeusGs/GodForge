using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.SendRegisterOtp;

public sealed class SendRegisterOtpCommandHandler : IRequestHandler<SendRegisterOtpCommand, Result>
{
    private readonly IUserRepository _users;
    private readonly ICacheService _cacheService;
    private readonly IEmailService _emailService;

    public SendRegisterOtpCommandHandler(
        IUserRepository users,
        ICacheService cacheService,
        IEmailService emailService)
    {
        _users = users;
        _cacheService = cacheService;
        _emailService = emailService;
    }

    public async Task<Result> Handle(SendRegisterOtpCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            return Result.Failure(ApplicationError.Conflict("AUTH_EMAIL_EXISTS", "Email is already in use."));
        }

        // Generate a 6-digit random code securely
        var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        // Store the OTP in cache with 5 minutes expiration
        var cacheKey = $"otp:register:{request.Email.Trim().ToLowerInvariant()}";
        await _cacheService.SetAsync(cacheKey, otp, TimeSpan.FromMinutes(5), cancellationToken: cancellationToken);

        // Send email containing the OTP
        var subject = "GodForge - Email Verification OTP";
        var body = $@"
            <div style=""font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e4e4e7; border-radius: 8px; background-color: #fafafa;"">
                <h2 style=""color: #0f172a; margin-bottom: 16px;"">Verify your email address</h2>
                <p style=""color: #334155; font-size: 16px; line-height: 24px;"">Thank you for signing up for GodForge. Please use the following One-Time Password (OTP) to verify your account:</p>
                <div style=""background-color: #f1f5f9; border: 1px solid #cbd5e1; border-radius: 6px; padding: 12px; margin: 24px 0; text-align: center;"">
                    <span style=""font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #10b981;"">{otp}</span>
                </div>
                <p style=""color: #64748b; font-size: 14px; line-height: 20px;"">This code will expire in 5 minutes. If you did not make this request, you can safely ignore this email.</p>
                <hr style=""border: 0; border-top: 1px solid #e2e8f0; margin: 30px 0;"" />
                <p style=""color: #94a3b8; font-size: 12px; text-align: center;"">© {DateTime.UtcNow.Year} GodForge. All rights reserved.</p>
            </div>";

        await _emailService.SendEmailAsync(request.Email.Trim(), subject, body, cancellationToken);

        return Result.Success();
    }
}
