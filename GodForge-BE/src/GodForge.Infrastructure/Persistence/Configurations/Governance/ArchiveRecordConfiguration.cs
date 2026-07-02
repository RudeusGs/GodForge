using GodForge.Domain.Entities.Governance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Governance;

public sealed class ArchiveRecordConfiguration : IEntityTypeConfiguration<ArchiveRecord>
{
    public void Configure(EntityTypeBuilder<ArchiveRecord> builder)
    {
        builder.ToTable("archive_records", "governance");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(r => r.SourceTable).HasColumnName("source_table").HasMaxLength(120).IsRequired();
        builder.Property(r => r.SourceId).HasColumnName("source_id").HasColumnType("uuid");
        builder.Property(r => r.ArtifactId).HasColumnName("artifact_id").HasColumnType("uuid");
        
        builder.Property(r => r.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz");
    }
}
