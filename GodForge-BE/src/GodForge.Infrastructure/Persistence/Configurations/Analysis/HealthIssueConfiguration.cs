using GodForge.Domain.Entities.Analysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Analysis;

public sealed class HealthIssueConfiguration : IEntityTypeConfiguration<HealthIssue>
{
    public void Configure(EntityTypeBuilder<HealthIssue> builder)
    {
        builder.ToTable("health_issues", "analysis");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(i => i.ReportId).HasColumnName("report_id").HasColumnType("uuid").IsRequired();
        builder.Property(i => i.RuleId).HasColumnName("rule_id").HasColumnType("uuid");
        builder.Property(i => i.IssueType).HasColumnName("issue_type").HasMaxLength(80).IsRequired();
        builder.Property(i => i.Severity).HasColumnName("severity").HasMaxLength(20).IsRequired();
        builder.Property(i => i.FilePath).HasColumnName("file_path").HasMaxLength(800);
        builder.Property(i => i.NodePath).HasColumnName("node_path").HasMaxLength(1000);
        builder.Property(i => i.Message).HasColumnName("message").HasColumnType("text").IsRequired();
        builder.Property(i => i.DetailsJson).HasColumnName("details").HasColumnType("jsonb");
        builder.Property(i => i.IsSuppressed).HasColumnName("is_suppressed").IsRequired();

        builder.Property(i => i.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<HealthReport>().WithMany().HasForeignKey(i => i.ReportId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<HealthRule>().WithMany().HasForeignKey(i => i.RuleId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => new { i.ReportId, i.Severity }).HasDatabaseName("ix_health_issues_report_severity");
        builder.HasIndex(i => i.FilePath).HasDatabaseName("ix_health_issues_file");
    }
}
