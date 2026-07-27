using GodForge.Domain.Entities.Analysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Analysis;

public sealed class AiAnalysisRunConfiguration : IEntityTypeConfiguration<AiAnalysisRun>
{
    public void Configure(EntityTypeBuilder<AiAnalysisRun> builder)
    {
        builder.ToTable("ai_analysis_runs", "analysis");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(x => x.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.CommitSha).HasColumnName("commit_sha").HasMaxLength(80).IsRequired();
        builder.Property(x => x.AnalysisProfile).HasColumnName("analysis_profile").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Model).HasColumnName("model").HasMaxLength(120).IsRequired();
        builder.Property(x => x.PromptVersion).HasColumnName("prompt_version").HasMaxLength(80).IsRequired();
        builder.Property(x => x.InputHash).HasColumnName("input_hash").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Summary).HasColumnName("summary").HasColumnType("text");
        builder.Property(x => x.RawArtifactKey).HasColumnName("raw_artifact_key").HasMaxLength(500);
        builder.Property(x => x.InputTokenCount).HasColumnName("input_token_count");
        builder.Property(x => x.OutputTokenCount).HasColumnName("output_token_count");
        builder.Property(x => x.EstimatedCost).HasColumnName("estimated_cost").HasPrecision(18, 6);
        builder.Property(x => x.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        builder.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(100);

        // Failed/degraded runs are retained for audit and may be retried. Only completed
        // runs are reused by the repository query, so this index must not be unique.
        builder.HasIndex(x => new
        {
            x.RepositoryId,
            x.CommitSha,
            x.AnalysisProfile,
            x.Provider,
            x.Model,
            x.PromptVersion,
            x.InputHash,
            x.Status
        })
            .HasDatabaseName("ix_ai_analysis_runs_cache");

        builder.HasIndex(x => new { x.ProjectId, x.StartedAt })
            .HasDatabaseName("ix_ai_analysis_runs_project_started");
    }
}
