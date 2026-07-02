using GodForge.Domain.Entities.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Metadata;

public sealed class SceneNodeReferenceConfiguration : IEntityTypeConfiguration<SceneNodeReference>
{
    public void Configure(EntityTypeBuilder<SceneNodeReference> builder)
    {
        builder.ToTable("scene_node_references", "metadata");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(r => r.SceneNodeId).HasColumnName("scene_node_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.PropertyName).HasColumnName("property_name").HasMaxLength(255).IsRequired();
        builder.Property(r => r.ReferenceType).HasColumnName("reference_type").HasMaxLength(40).IsRequired();
        builder.Property(r => r.TargetPath).HasColumnName("target_path").HasMaxLength(800);
        builder.Property(r => r.TargetUid).HasColumnName("target_uid").HasMaxLength(120);
        builder.Property(r => r.TargetExists).HasColumnName("target_exists").IsRequired();

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<SceneNode>().WithMany().HasForeignKey(r => r.SceneNodeId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.SceneNodeId).HasDatabaseName("ix_scene_node_references_node");
        builder.HasIndex(r => r.TargetPath).HasDatabaseName("ix_scene_node_references_target");
    }
}
