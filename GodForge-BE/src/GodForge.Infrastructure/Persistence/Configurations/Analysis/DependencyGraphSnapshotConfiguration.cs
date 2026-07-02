using GodForge.Domain.Entities.Analysis;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Analysis;

public sealed class DependencyGraphSnapshotConfiguration : IEntityTypeConfiguration<DependencyGraphSnapshot>
{
    public void Configure(EntityTypeBuilder<DependencyGraphSnapshot> builder)
    {
        builder.ToTable("dependency_graph_snapshots", "analysis");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(s => s.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.SnapshotId).HasColumnName("snapshot_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.AnalysisRunId).HasColumnName("analysis_run_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.GraphHash).HasColumnName("graph_hash").HasMaxLength(80).IsRequired();
        builder.Property(s => s.NodeCount).HasColumnName("node_count").IsRequired();
        builder.Property(s => s.EdgeCount).HasColumnName("edge_count").IsRequired();
        builder.Property(s => s.CycleCount).HasColumnName("cycle_count").IsRequired();
        
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Project>().WithMany().HasForeignKey(s => s.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<GitRepository>().WithMany().HasForeignKey(s => s.RepositoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RepositorySnapshot>().WithMany().HasForeignKey(s => s.SnapshotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AnalysisRun>().WithMany().HasForeignKey(s => s.AnalysisRunId).OnDelete(DeleteBehavior.Cascade);
    }
}
