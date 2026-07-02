using GodForge.Domain.Entities.Governance;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Governance;

public sealed class PurgeRequestConfiguration : IEntityTypeConfiguration<PurgeRequest>
{
    public void Configure(EntityTypeBuilder<PurgeRequest> builder)
    {
        builder.ToTable("purge_requests", "governance");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(r => r.TargetType).HasColumnName("target_type").HasMaxLength(80).IsRequired();
        builder.Property(r => r.TargetId).HasColumnName("target_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.Reason).HasColumnName("reason").HasColumnType("text").IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(r => r.RequestedBy).HasColumnName("requested_by").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.ApprovedBy).HasColumnName("approved_by").HasColumnType("uuid");

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");

        builder.HasOne<User>().WithMany().HasForeignKey(r => r.RequestedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.ApprovedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
