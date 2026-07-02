using GodForge.Domain.Entities.Admin;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Admin;

public sealed class AdminActionConfiguration : IEntityTypeConfiguration<AdminAction>
{
    public void Configure(EntityTypeBuilder<AdminAction> builder)
    {
        builder.ToTable("admin_actions", "audit"); // Documented as audit.admin_actions

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(a => a.AdminUserId).HasColumnName("admin_user_id").HasColumnType("uuid").IsRequired();
        builder.Property(a => a.TargetType).HasColumnName("target_type").HasMaxLength(100).IsRequired();
        builder.Property(a => a.TargetId).HasColumnName("target_id").HasColumnType("uuid");
        builder.Property(a => a.Action).HasColumnName("action").HasConversion<string>().HasMaxLength(120).IsRequired();
        builder.Property(a => a.Reason).HasColumnName("reason").HasColumnType("text").IsRequired();
        builder.Property(a => a.Outcome).HasColumnName("outcome").HasMaxLength(30).IsRequired();
        builder.Property(a => a.CorrelationId).HasColumnName("correlation_id").HasMaxLength(80).IsRequired();

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(a => a.AdminUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
