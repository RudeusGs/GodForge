using GodForge.Domain.Entities.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Metadata;

public sealed class SceneConnectionConfiguration : IEntityTypeConfiguration<SceneConnection>
{
    public void Configure(EntityTypeBuilder<SceneConnection> builder)
    {
        builder.ToTable("scene_connections", "metadata");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(c => c.SceneId).HasColumnName("scene_id").HasColumnType("uuid").IsRequired();
        builder.Property(c => c.SignalName).HasColumnName("signal_name").HasMaxLength(255).IsRequired();
        builder.Property(c => c.FromNodePath).HasColumnName("from_node_path").HasMaxLength(1000).IsRequired();
        builder.Property(c => c.ToNodePath).HasColumnName("to_node_path").HasMaxLength(1000).IsRequired();
        builder.Property(c => c.MethodName).HasColumnName("method_name").HasMaxLength(255).IsRequired();
        builder.Property(c => c.Flags).HasColumnName("flags");
        
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Scene>().WithMany().HasForeignKey(c => c.SceneId).OnDelete(DeleteBehavior.Cascade);
    }
}
