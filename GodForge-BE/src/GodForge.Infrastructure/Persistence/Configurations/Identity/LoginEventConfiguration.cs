using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Identity;

public sealed class LoginEventConfiguration : IEntityTypeConfiguration<LoginEvent>
{
    public void Configure(EntityTypeBuilder<LoginEvent> builder)
    {
        builder.ToTable("login_events", "identity");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid");
        builder.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(e => e.DeviceName).HasColumnName("device_name").HasMaxLength(200);
        builder.Property(e => e.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.FailureReason).HasColumnName("failure_reason").HasMaxLength(255);
        
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.UserId, e.CreatedAt }).HasDatabaseName("ix_login_events_user").IsDescending(false, true);
        builder.HasIndex(e => new { e.IpAddress, e.CreatedAt }).HasDatabaseName("ix_login_events_ip").IsDescending(false, true);
    }
}
