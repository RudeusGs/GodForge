using GodForge.Domain.Entities.Analysis;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Analysis;

public sealed class HealthReportConfiguration : IEntityTypeConfiguration<HealthReport>
{
    public void Configure(EntityTypeBuilder<HealthReport> builder)
    {
        builder.ToTable("health_reports", "analysis");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(r => r.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.SnapshotId).HasColumnName("snapshot_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.AnalysisRunId).HasColumnName("analysis_run_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.JobId).HasColumnName("job_id").HasColumnType("uuid");
        builder.Property(r => r.Score).HasColumnName("score").IsRequired();
        builder.Property(r => r.TotalIssues).HasColumnName("total_issues").IsRequired();
        builder.Property(r => r.CriticalCount).HasColumnName("critical_count").IsRequired();
        builder.Property(r => r.WarningCount).HasColumnName("warning_count").IsRequired();
        builder.Property(r => r.InfoCount).HasColumnName("info_count").IsRequired();
        builder.Property(r => r.SummaryJson).HasColumnName("summary").HasColumnType("jsonb");

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Project>().WithMany().HasForeignKey(r => r.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<GitRepository>().WithMany().HasForeignKey(r => r.RepositoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RepositorySnapshot>().WithMany().HasForeignKey(r => r.SnapshotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AnalysisRun>().WithMany().HasForeignKey(r => r.AnalysisRunId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Job>().WithMany().HasForeignKey(r => r.JobId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => new { r.ProjectId, r.CreatedAt }).HasDatabaseName("ix_health_reports_project_created").IsDescending(false, true);
        builder.HasIndex(r => r.SnapshotId).HasDatabaseName("ix_health_reports_snapshot");
        builder.HasIndex(r => r.AnalysisRunId).HasDatabaseName("ux_health_reports_analysis_run").IsUnique();
    }
}
