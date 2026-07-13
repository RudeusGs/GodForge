using System.Security.Cryptography;
using System.Text;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.PasswordResetTokenHash is null || user.PasswordResetTokenExpiry is null)
        {
            return Result.Failure(ApplicationError.Validation("AUTH_INVALID_RESET_TOKEN", "Invalid or expired reset token."));
        }

        if (_clock.UtcNow > user.PasswordResetTokenExpiry.Value)
        {
            return Result.Failure(ApplicationError.Validation("AUTH_INVALID_RESET_TOKEN", "Invalid or expired reset token."));
        }

        using var sha256 = SHA256.Create();
        var tokenHashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(request.Token));
        var tokenHash = Convert.ToBase64String(tokenHashBytes);

        if (user.PasswordResetTokenHash != tokenHash)
        {
            return Result.Failure(ApplicationError.Validation("AUTH_INVALID_RESET_TOKEN", "Invalid or expired reset token."));
        }

        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatePassword(newPasswordHash, _clock.UtcNow);
        user.ClearPasswordResetToken();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

