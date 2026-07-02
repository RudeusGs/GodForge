using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Metadata;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Metadata;

public sealed class MetadataRunConfiguration : IEntityTypeConfiguration<MetadataRun>
{
    public void Configure(EntityTypeBuilder<MetadataRun> builder)
    {
        builder.ToTable("metadata_runs", "metadata");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(r => r.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.SnapshotId).HasColumnName("snapshot_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.JobId).HasColumnName("job_id").HasColumnType("uuid");
        builder.Property(r => r.RunType).HasColumnName("run_type").HasMaxLength(40).IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(r => r.SchemaVersion).HasColumnName("schema_version").HasMaxLength(20).IsRequired().HasDefaultValue("1.0");
        builder.Property(r => r.FileCount).HasColumnName("file_count").IsRequired();
        builder.Property(r => r.SceneCount).HasColumnName("scene_count").IsRequired();
        builder.Property(r => r.AssetCount).HasColumnName("asset_count").IsRequired();
        builder.Property(r => r.ScriptCount).HasColumnName("script_count").IsRequired();
        builder.Property(r => r.ResourceCount).HasColumnName("resource_count").IsRequired();
        builder.Property(r => r.DependencyCount).HasColumnName("dependency_count").IsRequired();
        builder.Property(r => r.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Project>().WithMany().HasForeignKey(r => r.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<GitRepository>().WithMany().HasForeignKey(r => r.RepositoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RepositorySnapshot>().WithMany().HasForeignKey(r => r.SnapshotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Job>().WithMany().HasForeignKey(r => r.JobId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => new { r.RepositoryId, r.CreatedAt }).HasDatabaseName("ix_metadata_runs_repo_created").IsDescending(false, true);
        builder.HasIndex(r => r.SnapshotId).HasDatabaseName("ux_metadata_runs_snapshot_completed").IsUnique().HasFilter("status = 'completed'");
    }
}
