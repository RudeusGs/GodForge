using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserSettingConfiguration : IEntityTypeConfiguration<UserSetting>
{
    public void Configure(EntityTypeBuilder<UserSetting> builder)
    {
        builder.ToTable("user_settings", "identity");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(s => s.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.Theme).HasColumnName("theme").HasMaxLength(20).IsRequired();
        builder.Property(s => s.NotificationInApp).HasColumnName("notification_in_app").IsRequired();
        builder.Property(s => s.NotificationEmail).HasColumnName("notification_email").IsRequired();
        builder.Property(s => s.NotificationDigest).HasColumnName("notification_digest").HasMaxLength(20).IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<User>().WithOne().HasForeignKey<UserSetting>(s => s.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.UserId).HasDatabaseName("ux_user_settings_user_id").IsUnique();
    }
}
