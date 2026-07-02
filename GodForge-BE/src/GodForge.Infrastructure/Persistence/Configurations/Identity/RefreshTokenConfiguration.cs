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
        builder.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
        builder.Property(t => t.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash").HasMaxLength(255);
        builder.Property(t => t.DeviceName).HasColumnName("device_name").HasMaxLength(255);
        builder.Property(t => t.IpAddress).HasColumnName("ip_address").HasMaxLength(45);

        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(t => t.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");

        builder.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TokenHash).HasDatabaseName("ux_refresh_tokens_hash").IsUnique();
        builder.HasIndex(x => new { x.UserId, x.ExpiresAt })
               .HasDatabaseName("ix_refresh_tokens_user_expires")
               .HasFilter("revoked_at IS NULL");
    }
}
