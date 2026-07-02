using GodForge.Domain.Entities.Collab;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Collab;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences", "collab");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(p => p.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(p => p.EventType).HasColumnName("event_type").HasConversion<string>().HasMaxLength(80).IsRequired();
        builder.Property(p => p.InApp).HasColumnName("in_app").IsRequired();
        builder.Property(p => p.Email).HasColumnName("email").IsRequired();

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.UserId, p.EventType }).HasDatabaseName("ux_notification_preferences_user_event").IsUnique();
    }
}
