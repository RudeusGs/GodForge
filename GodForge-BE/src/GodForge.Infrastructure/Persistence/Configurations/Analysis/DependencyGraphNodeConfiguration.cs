using GodForge.Domain.Entities.Analysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Analysis;

public sealed class DependencyGraphNodeConfiguration : IEntityTypeConfiguration<DependencyGraphNode>
{
    public void Configure(EntityTypeBuilder<DependencyGraphNode> builder)
    {
        builder.ToTable("dependency_graph_nodes", "analysis");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(n => n.GraphSnapshotId).HasColumnName("graph_snapshot_id").HasColumnType("uuid").IsRequired();
        builder.Property(n => n.NodeKey).HasColumnName("node_key").HasMaxLength(900).IsRequired();
        builder.Property(n => n.NodeType).HasColumnName("node_type").HasMaxLength(40).IsRequired();
        builder.Property(n => n.FilePath).HasColumnName("file_path").HasMaxLength(800);
        builder.Property(n => n.Label).HasColumnName("label").HasMaxLength(255).IsRequired();
        builder.Property(n => n.MetricsJson).HasColumnName("metrics").HasColumnType("jsonb");

        builder.HasOne<DependencyGraphSnapshot>().WithMany().HasForeignKey(n => n.GraphSnapshotId).OnDelete(DeleteBehavior.Cascade);
    }
}
