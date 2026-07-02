using GodForge.Domain.Entities.Governance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Governance;

public sealed class RetentionRunItemConfiguration : IEntityTypeConfiguration<RetentionRunItem>
{
    public void Configure(EntityTypeBuilder<RetentionRunItem> builder)
    {
        builder.ToTable("retention_run_items", "governance");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(i => i.RetentionRunId).HasColumnName("retention_run_id").HasColumnType("uuid").IsRequired();
        builder.Property(i => i.TargetTable).HasColumnName("target_table").HasMaxLength(120).IsRequired();
        builder.Property(i => i.TargetId).HasColumnName("target_id").HasColumnType("uuid");
        builder.Property(i => i.Action).HasColumnName("action").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(i => i.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(i => i.ErrorMessage).HasColumnName("error_message").HasColumnType("text");

        builder.HasOne<RetentionRun>().WithMany().HasForeignKey(i => i.RetentionRunId).OnDelete(DeleteBehavior.Restrict);
    }
}
