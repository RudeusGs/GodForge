using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Identity;

public sealed class AuthChallengeConfiguration : IEntityTypeConfiguration<AuthChallenge>
{
    public void Configure(EntityTypeBuilder<AuthChallenge> builder)
    {
        builder.ToTable("auth_challenges", "identity");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(320).IsRequired();
        builder.Property(x => x.Purpose).HasColumnName("purpose").HasMaxLength(40).IsRequired();
        builder.Property(x => x.SecretHash).HasColumnName("secret_hash").HasMaxLength(255).IsRequired();
        builder.Property(x => x.FailedAttempts).HasColumnName("failed_attempts").IsRequired();
        builder.Property(x => x.MaxAttempts).HasColumnName("max_attempts").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ResendAvailableAt).HasColumnName("resend_available_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ConsumedAt).HasColumnName("consumed_at").HasColumnType("timestamptz");
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");
        builder.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp").HasMaxLength(64).IsRequired().IsConcurrencyToken();

        builder.HasIndex(x => new { x.NormalizedEmail, x.Purpose, x.ExpiresAt })
            .HasDatabaseName("ix_auth_challenges_lookup");
        builder.HasIndex(x => x.SecretHash)
            .HasDatabaseName("ix_auth_challenges_secret_hash");
        builder.HasIndex(x => new { x.NormalizedEmail, x.Purpose })
            .HasDatabaseName("ux_auth_challenges_active_scope")
            .HasFilter("consumed_at IS NULL AND revoked_at IS NULL")
            .IsUnique();
    }
}
