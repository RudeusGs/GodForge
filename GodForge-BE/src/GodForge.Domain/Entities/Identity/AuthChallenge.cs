using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Identity;

public sealed class AuthChallenge : BaseEntity
{
    public string NormalizedEmail { get; private set; } = default!;
    public string Purpose { get; private set; } = default!;
    public string SecretHash { get; private set; } = default!;
    public int FailedAttempts { get; private set; }
    public int MaxAttempts { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset ResendAvailableAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string ConcurrencyStamp { get; private set; } = default!;

    private AuthChallenge() { }

    public static AuthChallenge Create(
        string normalizedEmail,
        string purpose,
        string secretHash,
        DateTimeOffset now,
        TimeSpan lifetime,
        TimeSpan resendCooldown,
        int maxAttempts = 5)
        => new()
        {
            Id = Guid.NewGuid(),
            NormalizedEmail = normalizedEmail,
            Purpose = purpose,
            SecretHash = secretHash,
            FailedAttempts = 0,
            MaxAttempts = maxAttempts,
            CreatedAt = now,
            UpdatedAt = now,
            ResendAvailableAt = now.Add(resendCooldown),
            ExpiresAt = now.Add(lifetime),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

    public bool IsActive(DateTimeOffset now)
        => ConsumedAt is null && RevokedAt is null && FailedAttempts < MaxAttempts && now < ExpiresAt;

    public bool IsInCooldown(DateTimeOffset now) => IsActive(now) && now < ResendAvailableAt;

    public void ReplaceSecret(string secretHash, DateTimeOffset now, TimeSpan lifetime, TimeSpan resendCooldown)
    {
        if (ConsumedAt is not null)
            throw new InvalidOperationException("A consumed challenge cannot be reused.");

        SecretHash = secretHash;
        FailedAttempts = 0;
        RevokedAt = null;
        UpdatedAt = now;
        ResendAvailableAt = now.Add(resendCooldown);
        ExpiresAt = now.Add(lifetime);
        TouchConcurrency();
    }

    public void RecordFailedAttempt(DateTimeOffset now)
    {
        if (ConsumedAt is not null || RevokedAt is not null)
            return;

        FailedAttempts++;
        UpdatedAt = now;
        if (FailedAttempts >= MaxAttempts)
            RevokedAt = now;
        TouchConcurrency();
    }

    public void Consume(DateTimeOffset now)
    {
        if (!IsActive(now))
            throw new InvalidOperationException("Challenge is no longer active.");

        ConsumedAt = now;
        UpdatedAt = now;
        TouchConcurrency();
    }

    public void Revoke(DateTimeOffset now)
    {
        if (ConsumedAt is null && RevokedAt is null)
        {
            RevokedAt = now;
            UpdatedAt = now;
            TouchConcurrency();
        }
    }

    private void TouchConcurrency() => ConcurrencyStamp = Guid.NewGuid().ToString("N");
}
