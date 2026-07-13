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

    public DateTimeOffset? DeletedAt { get; private set; }

    private User() { } // EF Core

    public static User Create(string email, string displayName, string passwordHash, DateTimeOffset now)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = displayName,
            PasswordHash = passwordHash,
            SystemRole = SystemRole.User,
            Status = UserStatus.Active,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateSystemRole(SystemRole role)
    {
        SystemRole = role;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (DeletedAt is not null) return;
        DeletedAt = now;
        Status = UserStatus.Deleted;
        UpdatedAt = now;
    }

    public void RecordLoginSuccess(DateTimeOffset now)
    {
        LastLoginAt = now;
        FailedLoginCount = 0;
        LockedUntil = null;
        UpdatedAt = now;
    }

    public void RecordLoginFailure(DateTimeOffset now, int maxFailedAccessAttempts, TimeSpan lockoutTimeSpan)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= maxFailedAccessAttempts)
        {
            LockedUntil = now.Add(lockoutTimeSpan);
            Status = UserStatus.Locked;
        }
        UpdatedAt = now;
    }

    public void UpdatePassword(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        PasswordChangedAt = now;
        SecurityStamp = Guid.NewGuid().ToString();
        UpdatedAt = now;
    }
}
