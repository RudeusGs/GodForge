using GodForge.Domain.Entities.Metadata;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Metadata;

public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("assets", "metadata");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(a => a.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(a => a.SnapshotId).HasColumnName("snapshot_id").HasColumnType("uuid").IsRequired();
        builder.Property(a => a.MetadataRunId).HasColumnName("metadata_run_id").HasColumnType("uuid").IsRequired();
        builder.Property(a => a.FilePath).HasColumnName("file_path").HasMaxLength(800).IsRequired();
        builder.Property(a => a.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        builder.Property(a => a.AssetType).HasColumnName("asset_type").HasMaxLength(40).IsRequired();
        builder.Property(a => a.FileSizeBytes).HasColumnName("file_size_bytes").HasColumnType("bigint").IsRequired();
        builder.Property(a => a.MimeType).HasColumnName("mime_type").HasMaxLength(120);
        builder.Property(a => a.DimensionsJson).HasColumnName("dimensions").HasColumnType("jsonb");
        builder.Property(a => a.ThumbnailArtifactId).HasColumnName("thumbnail_artifact_id").HasColumnType("uuid");
        builder.Property(a => a.FileHash).HasColumnName("file_hash").HasMaxLength(80).IsRequired();
        builder.Property(a => a.ImportedMetadataJson).HasColumnName("imported_metadata").HasColumnType("jsonb");

        builder.HasOne<GitRepository>().WithMany().HasForeignKey(a => a.RepositoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RepositorySnapshot>().WithMany().HasForeignKey(a => a.SnapshotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MetadataRun>().WithMany().HasForeignKey(a => a.MetadataRunId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Artifact>().WithMany().HasForeignKey(a => a.ThumbnailArtifactId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => new { a.SnapshotId, a.FilePath }).HasDatabaseName("ux_assets_snapshot_path").IsUnique();
        builder.HasIndex(a => new { a.SnapshotId, a.AssetType }).HasDatabaseName("ix_assets_snapshot_type");
    }
}
