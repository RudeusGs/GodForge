using GodForge.Domain.Entities.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Metadata;

public sealed class SceneNodePropertyConfiguration : IEntityTypeConfiguration<SceneNodeProperty>
{
    public void Configure(EntityTypeBuilder<SceneNodeProperty> builder)
    {
        builder.ToTable("scene_node_properties", "metadata");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(p => p.SceneNodeId).HasColumnName("scene_node_id").HasColumnType("uuid").IsRequired();
        builder.Property(p => p.PropertyName).HasColumnName("property_name").HasMaxLength(255).IsRequired();
        builder.Property(p => p.PropertyType).HasColumnName("property_type").HasMaxLength(80);
        builder.Property(p => p.PropertyValueJson).HasColumnName("property_value_json").HasColumnType("jsonb");
        builder.Property(p => p.PropertyValueHash).HasColumnName("property_value_hash").HasMaxLength(80);

        builder.HasOne<SceneNode>().WithMany().HasForeignKey(p => p.SceneNodeId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.SceneNodeId).HasDatabaseName("ix_scene_node_properties_node");
    }
}
