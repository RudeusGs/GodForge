using GodForge.Domain.Entities.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Metadata;

public sealed class SceneNodeConfiguration : IEntityTypeConfiguration<SceneNode>
{
    public void Configure(EntityTypeBuilder<SceneNode> builder)
    {
        builder.ToTable("scene_nodes", "metadata");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(n => n.SceneId).HasColumnName("scene_id").HasColumnType("uuid").IsRequired();
        builder.Property(n => n.NodePath).HasColumnName("node_path").HasMaxLength(1000).IsRequired();
        builder.Property(n => n.NodeName).HasColumnName("node_name").HasMaxLength(255).IsRequired();
        builder.Property(n => n.NodeType).HasColumnName("node_type").HasMaxLength(120).IsRequired();
        builder.Property(n => n.ParentPath).HasColumnName("parent_path").HasMaxLength(1000).IsRequired();
        builder.Property(n => n.Depth).HasColumnName("depth").IsRequired();
        builder.Property(n => n.NodeOrder).HasColumnName("node_order").IsRequired();
        builder.Property(n => n.ScriptPath).HasColumnName("script_path").HasMaxLength(800);
        builder.Property(n => n.Groups).HasColumnName("groups").HasColumnType("text[]");
        builder.Property(n => n.ImportantPropertiesJson).HasColumnName("important_properties").HasColumnType("jsonb");
        builder.Property(n => n.PropertiesJson).HasColumnName("properties").HasColumnType("jsonb");
        builder.Property(n => n.WarningCount).HasColumnName("warning_count").IsRequired();

        builder.HasOne<Scene>().WithMany().HasForeignKey(n => n.SceneId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.SceneId, n.NodePath }).HasDatabaseName("ux_scene_nodes_scene_path").IsUnique();
        builder.HasIndex(n => new { n.SceneId, n.NodeType }).HasDatabaseName("ix_scene_nodes_type");
        builder.HasIndex(n => new { n.SceneId, n.NodeOrder }).HasDatabaseName("ix_scene_nodes_order");
    }
}
