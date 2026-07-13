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

        string newPassword = GenerateRandomPassword(16);
        string hashedPassword = _passwordHasher.HashPassword(newPassword);

        user.UpdatePassword(hashedPassword, _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        string emailSubject = "GodForge - Password Reset";
        string emailBody = $@"
Hello {user.DisplayName},

Your GodForge password has been reset. 
Your new password is: {newPassword}

Please log in and change this password immediately.

Regards,
GodForge Team
";
        await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody, cancellationToken);

        return Result.Success();
    }

    private string GenerateRandomPassword(int length)
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string allChars = uppercase + lowercase + digits;

        var result = new StringBuilder(length);

        // Ensure at least one of each required type
        result.Append(uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)]);
        result.Append(lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)]);
        result.Append(digits[RandomNumberGenerator.GetInt32(digits.Length)]);

        for (int i = 3; i < length; i++)
        {
            result.Append(allChars[RandomNumberGenerator.GetInt32(allChars.Length)]);
        }

        // Shuffle
        var arr = result.ToString().ToCharArray();
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            var temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }
        
        return new string(arr);
    }
}
