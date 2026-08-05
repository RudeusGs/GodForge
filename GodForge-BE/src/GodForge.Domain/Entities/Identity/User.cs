using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Identity;

public sealed class User : BaseAuditableEntity, ISoftDeletable
{
    public string Email { get; private set; } = default!;
    public string NormalizedEmail { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public SystemRole SystemRole { get; private set; }
    public UserStatus Status { get; private set; }

    public DateTimeOffset? EmailVerifiedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public DateTimeOffset? PasswordChangedAt { get; private set; }

    public int FailedLoginCount { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }

    public string? AvatarUrl { get; private set; }
    public string SecurityStamp { get; private set; } = default!;
    public string ConcurrencyStamp { get; private set; } = default!;
    public long Version { get; private set; }

    public string? PasswordResetTokenHash { get; private set; }
    public DateTimeOffset? PasswordResetTokenExpiry { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    private User() { }

    public static User Create(string email, string displayName, string passwordHash, DateTimeOffset now)
    {
        var normalizedEmail = NormalizeEmail(email);
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash,
            SystemRole = SystemRole.User,
            Status = UserStatus.Active,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    public void MarkEmailVerified(DateTimeOffset now)
    {
        if (EmailVerifiedAt is null)
        {
            EmailVerifiedAt = now;
            Touch(now);
        }
    }

    public void UpdateSystemRole(SystemRole role)
    {
        SystemRole = role;
        Version++;
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (DeletedAt is not null) return;
        DeletedAt = now;
        Status = UserStatus.Deleted;
        Touch(now);
    }

    public void RecordLoginSuccess(DateTimeOffset now)
    {
        LastLoginAt = now;
        FailedLoginCount = 0;
        LockedUntil = null;
        if (Status == UserStatus.Locked)
            Status = UserStatus.Active;
        Touch(now);
    }

    public void RecordLoginFailure(DateTimeOffset now, int maxFailedAccessAttempts, TimeSpan lockoutTimeSpan)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= maxFailedAccessAttempts)
        {
            LockedUntil = now.Add(lockoutTimeSpan);
            Status = UserStatus.Locked;
        }
        Touch(now);
    }

    public void Unlock(DateTimeOffset now)
    {
        FailedLoginCount = 0;
        LockedUntil = null;
        Status = UserStatus.Active;
        Touch(now);
    }

    public void UpdatePassword(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        PasswordChangedAt = now;
        SecurityStamp = Guid.NewGuid().ToString("N");
        Touch(now);
    }

    public void SetPasswordResetToken(string tokenHash, DateTimeOffset expiry)
    {
        PasswordResetTokenHash = tokenHash;
        PasswordResetTokenExpiry = expiry;
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiry = null;
    }

    private void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAt = now;
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }
}
