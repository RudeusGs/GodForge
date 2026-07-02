using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions", "identity");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(s => s.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.DeviceName).HasColumnName("device_name").HasMaxLength(200);
        builder.Property(s => s.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(s => s.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.LastSeenAt).HasColumnName("last_seen_at").HasColumnType("timestamptz");
        builder.Property(s => s.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");
        builder.Property(s => s.RevokedReason).HasColumnName("revoked_reason").HasMaxLength(100);

        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.UserId, s.ExpiresAt })
               .HasDatabaseName("ix_user_sessions_user_active")
               .HasFilter("revoked_at IS NULL");
    }
}
