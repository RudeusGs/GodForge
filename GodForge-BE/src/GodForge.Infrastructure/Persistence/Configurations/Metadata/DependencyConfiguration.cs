using GodForge.Domain.Entities.Metadata;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Metadata;

public sealed class DependencyConfiguration : IEntityTypeConfiguration<Dependency>
{
    public void Configure(EntityTypeBuilder<Dependency> builder)
    {
        builder.ToTable("dependencies", "metadata");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(d => d.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(d => d.SnapshotId).HasColumnName("snapshot_id").HasColumnType("uuid").IsRequired();
        builder.Property(d => d.MetadataRunId).HasColumnName("metadata_run_id").HasColumnType("uuid").IsRequired();
        builder.Property(d => d.SourceType).HasColumnName("source_type").HasMaxLength(40).IsRequired();
        builder.Property(d => d.SourcePath).HasColumnName("source_path").HasMaxLength(800).IsRequired();
        builder.Property(d => d.TargetType).HasColumnName("target_type").HasMaxLength(40).IsRequired();
        builder.Property(d => d.TargetPath).HasColumnName("target_path").HasMaxLength(800).IsRequired();
        builder.Property(d => d.Relation).HasColumnName("relation").HasMaxLength(40).IsRequired();
        builder.Property(d => d.IsMissing).HasColumnName("is_missing").IsRequired();

        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<GitRepository>().WithMany().HasForeignKey(d => d.RepositoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RepositorySnapshot>().WithMany().HasForeignKey(d => d.SnapshotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MetadataRun>().WithMany().HasForeignKey(d => d.MetadataRunId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.SnapshotId, d.SourcePath, d.TargetPath, d.Relation }).HasDatabaseName("ux_dependencies_snapshot_edge").IsUnique();
        builder.HasIndex(d => new { d.SnapshotId, d.SourceType, d.SourcePath }).HasDatabaseName("ix_dependencies_source");
        builder.HasIndex(d => new { d.SnapshotId, d.TargetType, d.TargetPath }).HasDatabaseName("ix_dependencies_target");
        builder.HasIndex(d => new { d.SnapshotId, d.IsMissing }).HasDatabaseName("ix_dependencies_missing");
    }
}
