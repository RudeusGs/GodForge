using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Identity;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", "identity");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(t => t.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(t => t.SessionId).HasColumnName("session_id").HasColumnType("uuid").IsRequired();
        builder.Property(t => t.FamilyId).HasColumnName("family_id").HasColumnType("uuid").IsRequired();
        builder.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
        builder.Property(t => t.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash").HasMaxLength(255);
        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(t => t.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");
        builder.Property(t => t.RevokedReason).HasColumnName("revoked_reason").HasMaxLength(100);
        builder.Property(t => t.ConcurrencyStamp).HasColumnName("concurrency_stamp").HasMaxLength(64).IsRequired().IsConcurrencyToken();
        builder.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserSession>().WithMany().HasForeignKey(t => t.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.TokenHash).HasDatabaseName("ux_refresh_tokens_hash").IsUnique();
        builder.HasIndex(x => new { x.SessionId, x.ExpiresAt })
            .HasDatabaseName("ix_refresh_tokens_session_expires")
            .HasFilter("revoked_at IS NULL");
        builder.HasIndex(x => x.FamilyId).HasDatabaseName("ix_refresh_tokens_family");
    }
}
