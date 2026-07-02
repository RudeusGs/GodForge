using GodForge.Domain.Entities.Governance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Governance;

public sealed class RetentionRunConfiguration : IEntityTypeConfiguration<RetentionRun>
{
    public void Configure(EntityTypeBuilder<RetentionRun> builder)
    {
        builder.ToTable("retention_runs", "governance");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(r => r.PolicyId).HasColumnName("policy_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(r => r.AffectedCount).HasColumnName("affected_count").IsRequired();
        
        builder.Property(r => r.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");

        builder.HasOne<RetentionPolicy>().WithMany().HasForeignKey(r => r.PolicyId).OnDelete(DeleteBehavior.Cascade);
    }
}
