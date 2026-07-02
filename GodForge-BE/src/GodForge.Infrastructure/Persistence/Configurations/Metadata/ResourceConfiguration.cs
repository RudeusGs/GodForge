using GodForge.Domain.Entities.Metadata;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Metadata;

public sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("resources", "metadata");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(r => r.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.SnapshotId).HasColumnName("snapshot_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.MetadataRunId).HasColumnName("metadata_run_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.FilePath).HasColumnName("file_path").HasMaxLength(800).IsRequired();
        builder.Property(r => r.ResourceType).HasColumnName("resource_type").HasMaxLength(120).IsRequired();
        builder.Property(r => r.Uid).HasColumnName("uid").HasMaxLength(120);
        builder.Property(r => r.FileHash).HasColumnName("file_hash").HasMaxLength(80).IsRequired();
        builder.Property(r => r.PropertiesJson).HasColumnName("properties").HasColumnType("jsonb");

        builder.HasOne<Repository>().WithMany().HasForeignKey(r => r.RepositoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RepositorySnapshot>().WithMany().HasForeignKey(r => r.SnapshotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MetadataRun>().WithMany().HasForeignKey(r => r.MetadataRunId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.SnapshotId, r.FilePath }).HasDatabaseName("ux_resources_snapshot_path").IsUnique();
    }
}
