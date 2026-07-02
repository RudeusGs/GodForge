using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Identity;

public sealed class UserProfile : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public string? Bio { get; private set; }
    public string? Timezone { get; private set; }
    public string? Locale { get; private set; }
    public string? CompanyName { get; private set; }

    private UserProfile() { } // EF Core

    public static UserProfile Create(Guid userId, string? bio, string? timezone, string? locale, string? companyName, DateTimeOffset now)
    {
        return new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Bio = bio,
            Timezone = timezone,
            Locale = locale,
            CompanyName = companyName,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string? bio, string? timezone, string? locale, string? companyName, DateTimeOffset now)
    {
        Bio = bio;
        Timezone = timezone;
        Locale = locale;
        CompanyName = companyName;
        UpdatedAt = now;
    }
}
