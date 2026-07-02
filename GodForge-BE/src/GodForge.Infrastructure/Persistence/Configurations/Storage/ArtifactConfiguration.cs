using GodForge.Domain.Entities.Storage;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Storage;

public sealed class ArtifactConfiguration : IEntityTypeConfiguration<Artifact>
{
    public void Configure(EntityTypeBuilder<Artifact> builder)
    {
        builder.ToTable("artifacts", "storage");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(a => a.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(a => a.Type).HasColumnName("type").HasMaxLength(40).IsRequired();
        builder.Property(a => a.Size).HasColumnName("size").HasColumnType("bigint").IsRequired();
        builder.Property(a => a.ObjectPath).HasColumnName("object_path").HasMaxLength(500).IsRequired();
        builder.Property(a => a.MimeType).HasColumnName("mime_type").HasMaxLength(120);
        builder.Property(a => a.CreatedBy).HasColumnName("created_by").HasColumnType("uuid").IsRequired();
        
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Project>().WithMany().HasForeignKey(a => a.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.CreatedBy).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.ProjectId, a.Name }).HasDatabaseName("ux_artifacts_project_name").IsUnique();
        builder.HasIndex(a => new { a.ProjectId, a.Type }).HasDatabaseName("ix_artifacts_project_type");
    }
}
