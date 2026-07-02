using GodForge.Domain.Entities.Metadata;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Metadata;

public sealed class SceneConfiguration : IEntityTypeConfiguration<Scene>
{
    public void Configure(EntityTypeBuilder<Scene> builder)
    {
        builder.ToTable("scenes", "metadata");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(s => s.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.SnapshotId).HasColumnName("snapshot_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.MetadataRunId).HasColumnName("metadata_run_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.FilePath).HasColumnName("file_path").HasMaxLength(800).IsRequired();
        builder.Property(s => s.SceneName).HasColumnName("scene_name").HasMaxLength(255).IsRequired();
        builder.Property(s => s.FormatVersion).HasColumnName("format_version");
        builder.Property(s => s.Uid).HasColumnName("uid").HasMaxLength(120);
        builder.Property(s => s.LoadSteps).HasColumnName("load_steps");
        builder.Property(s => s.RootNodeName).HasColumnName("root_node_name").HasMaxLength(255);
        builder.Property(s => s.RootNodeType).HasColumnName("root_node_type").HasMaxLength(120);
        builder.Property(s => s.NodeCount).HasColumnName("node_count").IsRequired();
        builder.Property(s => s.FileHash).HasColumnName("file_hash").HasMaxLength(80).IsRequired();
        builder.Property(s => s.ParsedAt).HasColumnName("parsed_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Repository>().WithMany().HasForeignKey(s => s.RepositoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RepositorySnapshot>().WithMany().HasForeignKey(s => s.SnapshotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MetadataRun>().WithMany().HasForeignKey(s => s.MetadataRunId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.SnapshotId, s.FilePath }).HasDatabaseName("ux_scenes_snapshot_path").IsUnique();
        builder.HasIndex(s => new { s.RepositoryId, s.FilePath }).HasDatabaseName("ix_scenes_repo_path");
    }
}
