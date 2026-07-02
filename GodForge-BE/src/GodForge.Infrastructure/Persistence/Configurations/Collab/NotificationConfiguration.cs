using GodForge.Domain.Entities.Collab;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Collab;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", "collab");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(n => n.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(n => n.ProjectId).HasColumnName("project_id").HasColumnType("uuid");
        builder.Property(n => n.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(n => n.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
        builder.Property(n => n.Message).HasColumnName("message").HasColumnType("text").IsRequired();
        builder.Property(n => n.Link).HasColumnName("link").HasMaxLength(500);
        builder.Property(n => n.IsRead).HasColumnName("is_read").IsRequired();
        
        builder.Property(n => n.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(n => n.ProjectId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => new { n.UserId, n.CreatedAt }).HasDatabaseName("ix_notifications_user_time").IsDescending(false, true);
        builder.HasIndex(n => new { n.UserId, n.IsRead }).HasDatabaseName("ix_notifications_user_read");
    }
}
