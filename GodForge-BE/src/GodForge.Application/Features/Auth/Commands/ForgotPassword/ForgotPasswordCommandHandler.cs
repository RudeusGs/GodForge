using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using MediatR;
using System.Security.Cryptography;
using System.Text;

namespace GodForge.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            // Do not reveal that the email doesn't exist
            return Result.Success();
        }

        var tokenBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        var token = Convert.ToBase64String(tokenBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        using var sha256 = SHA256.Create();
        var tokenHashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        var tokenHash = Convert.ToBase64String(tokenHashBytes);

        user.SetPasswordResetToken(tokenHash, _clock.UtcNow.AddHours(1));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        string resetLink = $"http://localhost:5173/auth/reset-password?token={token}&email={Uri.EscapeDataString(user.Email)}";

        string emailSubject = "GodForge - Password Reset";
        string emailBody = $@"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e4e4e7; border-radius: 8px; background-color: #fafafa;"">
    <h2 style=""color: #0f172a; margin-bottom: 16px;"">Password Reset Request</h2>
    <p style=""color: #334155; font-size: 16px; line-height: 24px;"">Hello {user.DisplayName},</p>
    <p style=""color: #334155; font-size: 16px; line-height: 24px;"">You recently requested to reset your password for your GodForge account. Click the button below to reset it:</p>
    <div style=""margin: 24px 0; text-align: center;"">
        <a href=""{resetLink}"" style=""background-color: #3b82f6; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 16px;"">Reset Password</a>
    </div>
    <p style=""color: #64748b; font-size: 14px; line-height: 20px;"">Or copy and paste this link into your browser:</p>
    <p style=""color: #3b82f6; font-size: 14px; word-break: break-all;"">{resetLink}</p>
    <p style=""color: #64748b; font-size: 14px; line-height: 20px;"">This link will expire in 1 hour. If you did not make this request, you can safely ignore this email.</p>
    <hr style=""border: 0; border-top: 1px solid #e2e8f0; margin: 30px 0;"" />
    <p style=""color: #94a3b8; font-size: 12px; text-align: center;"">© {DateTime.UtcNow.Year} GodForge. All rights reserved.</p>
</div>";

        await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody, cancellationToken);

        return Result.Success();
    }
}
