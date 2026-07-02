using GodForge.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Admin;

public sealed class DataBackfillRunConfiguration : IEntityTypeConfiguration<DataBackfillRun>
{
    public void Configure(EntityTypeBuilder<DataBackfillRun> builder)
    {
        builder.ToTable("data_backfill_runs", "admin");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(r => r.ProcessedCount).HasColumnName("processed_count").IsRequired();
        builder.Property(r => r.FailedCount).HasColumnName("failed_count").IsRequired();
        builder.Property(r => r.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        builder.Property(r => r.MetadataJson).HasColumnName("metadata").HasColumnType("jsonb");
    }
}
