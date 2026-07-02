using GodForge.Domain.Entities.Storage;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Storage;

public sealed class ArtifactVersionConfiguration : IEntityTypeConfiguration<ArtifactVersion>
{
    public void Configure(EntityTypeBuilder<ArtifactVersion> builder)
    {
        builder.ToTable("artifact_versions", "storage");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(v => v.ArtifactId).HasColumnName("artifact_id").HasColumnType("uuid").IsRequired();
        builder.Property(v => v.VersionNumber).HasColumnName("version_number").IsRequired();
        builder.Property(v => v.Size).HasColumnName("size").HasColumnType("bigint").IsRequired();
        builder.Property(v => v.ObjectPath).HasColumnName("object_path").HasMaxLength(500).IsRequired();
        builder.Property(v => v.CreatedBy).HasColumnName("created_by").HasColumnType("uuid").IsRequired();
        
        builder.Property(v => v.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Artifact>().WithMany().HasForeignKey(v => v.ArtifactId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(v => v.CreatedBy).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.ArtifactId, v.VersionNumber }).HasDatabaseName("ux_artifact_versions_artifact_number").IsUnique();
    }
}
