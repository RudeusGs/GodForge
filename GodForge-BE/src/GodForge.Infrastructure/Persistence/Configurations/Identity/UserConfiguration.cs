using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "identity");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(u => u.Email).HasColumnName("email").HasConversion<string>().HasMaxLength(255).IsRequired();
        builder.Property(u => u.NormalizedEmail).HasColumnName("normalized_email").HasConversion<string>().HasMaxLength(255).IsRequired();
        builder.Property(u => u.DisplayName).HasColumnName("display_name").HasConversion<string>().HasMaxLength(120).IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasConversion<string>().HasMaxLength(255).IsRequired();
        builder.Property(u => u.SystemRole).HasColumnName("system_role").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(u => u.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        
        builder.Property(u => u.EmailVerifiedAt).HasColumnName("email_verified_at").HasColumnType("timestamptz");
        builder.Property(u => u.LastLoginAt).HasColumnName("last_login_at").HasColumnType("timestamptz");
        builder.Property(u => u.PasswordChangedAt).HasColumnName("password_changed_at").HasColumnType("timestamptz");
        
        builder.Property(u => u.FailedLoginCount).HasColumnName("failed_login_count").HasDefaultValue(0);
        builder.Property(u => u.LockedUntil).HasColumnName("locked_until").HasColumnType("timestamptz");
        
        builder.Property(u => u.AvatarUrl).HasColumnName("avatar_url").HasConversion<string>().HasMaxLength(500);
        builder.Property(u => u.SecurityStamp).HasColumnName("security_stamp").HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(u => u.ConcurrencyStamp).HasColumnName("concurrency_stamp").HasConversion<string>().HasMaxLength(100).IsRequired().IsConcurrencyToken();
        
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasQueryFilter(u => u.DeletedAt == null);

        builder.HasIndex(x => x.NormalizedEmail).HasDatabaseName("ux_users_normalized_email").IsUnique();
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_users_status");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_users_created_at").IsDescending();
    }
}
