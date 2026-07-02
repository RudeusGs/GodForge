using GodForge.Domain.Entities.Metadata;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Metadata;

public sealed class ScriptConfiguration : IEntityTypeConfiguration<Script>
{
    public void Configure(EntityTypeBuilder<Script> builder)
    {
        builder.ToTable("scripts", "metadata");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(s => s.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.SnapshotId).HasColumnName("snapshot_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.MetadataRunId).HasColumnName("metadata_run_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.FilePath).HasColumnName("file_path").HasMaxLength(800).IsRequired();
        builder.Property(s => s.ClassName).HasColumnName("class_name").HasMaxLength(255);
        builder.Property(s => s.ExtendsType).HasColumnName("extends_type").HasMaxLength(255);
        builder.Property(s => s.LineCount).HasColumnName("line_count").IsRequired();
        builder.Property(s => s.PublicMethodCount).HasColumnName("public_method_count").IsRequired();
        builder.Property(s => s.SignalCount).HasColumnName("signal_count").IsRequired();
        builder.Property(s => s.ExportedPropertyCount).HasColumnName("exported_property_count").IsRequired();
        builder.Property(s => s.FileHash).HasColumnName("file_hash").HasMaxLength(80).IsRequired();
        builder.Property(s => s.ParseSummaryJson).HasColumnName("parse_summary").HasColumnType("jsonb");

        builder.HasOne<GitRepository>().WithMany().HasForeignKey(s => s.RepositoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RepositorySnapshot>().WithMany().HasForeignKey(s => s.SnapshotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MetadataRun>().WithMany().HasForeignKey(s => s.MetadataRunId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.SnapshotId, s.FilePath }).HasDatabaseName("ux_scripts_snapshot_path").IsUnique();
        builder.HasIndex(s => new { s.SnapshotId, s.ClassName }).HasDatabaseName("ix_scripts_class_name");
    }
}
