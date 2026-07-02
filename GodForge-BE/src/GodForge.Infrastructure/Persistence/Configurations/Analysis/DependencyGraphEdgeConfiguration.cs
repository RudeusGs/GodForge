using GodForge.Domain.Entities.Analysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Analysis;

public sealed class DependencyGraphEdgeConfiguration : IEntityTypeConfiguration<DependencyGraphEdge>
{
    public void Configure(EntityTypeBuilder<DependencyGraphEdge> builder)
    {
        builder.ToTable("dependency_graph_edges", "analysis");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(e => e.GraphSnapshotId).HasColumnName("graph_snapshot_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.SourceNodeKey).HasColumnName("source_node_key").HasMaxLength(900).IsRequired();
        builder.Property(e => e.TargetNodeKey).HasColumnName("target_node_key").HasMaxLength(900).IsRequired();
        builder.Property(e => e.Relation).HasColumnName("relation").HasMaxLength(40).IsRequired();
        builder.Property(e => e.Weight).HasColumnName("weight").HasColumnType("numeric(12,4)").IsRequired();

        builder.HasOne<DependencyGraphSnapshot>().WithMany().HasForeignKey(e => e.GraphSnapshotId).OnDelete(DeleteBehavior.Cascade);
    }
}
