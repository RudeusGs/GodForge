using GodForge.Domain.Entities.Metadata;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Metadata;

public sealed class ParserDiagnosticConfiguration : IEntityTypeConfiguration<ParserDiagnostic>
{
    public void Configure(EntityTypeBuilder<ParserDiagnostic> builder)
    {
        builder.ToTable("parser_diagnostics", "metadata");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(d => d.MetadataRunId).HasColumnName("metadata_run_id").HasColumnType("uuid").IsRequired();
        builder.Property(d => d.SnapshotId).HasColumnName("snapshot_id").HasColumnType("uuid").IsRequired();
        builder.Property(d => d.FilePath).HasColumnName("file_path").HasMaxLength(800).IsRequired();
        builder.Property(d => d.Severity).HasColumnName("severity").HasMaxLength(20).IsRequired();
        builder.Property(d => d.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(d => d.Message).HasColumnName("message").HasColumnType("text").IsRequired();
        builder.Property(d => d.Line).HasColumnName("line");
        builder.Property(d => d.Column).HasColumnName("column");
        builder.Property(d => d.DetailsJson).HasColumnName("details").HasColumnType("jsonb");

        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<MetadataRun>().WithMany().HasForeignKey(d => d.MetadataRunId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RepositorySnapshot>().WithMany().HasForeignKey(d => d.SnapshotId).OnDelete(DeleteBehavior.Restrict);
    }
}
