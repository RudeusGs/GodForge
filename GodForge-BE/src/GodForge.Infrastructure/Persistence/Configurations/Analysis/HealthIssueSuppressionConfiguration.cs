using GodForge.Domain.Entities.Analysis;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Analysis;

public sealed class HealthIssueSuppressionConfiguration : IEntityTypeConfiguration<HealthIssueSuppression>
{
    public void Configure(EntityTypeBuilder<HealthIssueSuppression> builder)
    {
        builder.ToTable("health_issue_suppressions", "analysis");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(s => s.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.RuleCode).HasColumnName("rule_code").HasMaxLength(100).IsRequired();
        builder.Property(s => s.FilePath).HasColumnName("file_path").HasMaxLength(800);
        builder.Property(s => s.NodePath).HasColumnName("node_path").HasMaxLength(1000);
        builder.Property(s => s.Reason).HasColumnName("reason").HasColumnType("text").IsRequired();
        builder.Property(s => s.SuppressedBy).HasColumnName("suppressed_by").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.SuppressedUntil).HasColumnName("suppressed_until").HasColumnType("timestamptz");

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");

        builder.HasOne<Project>().WithMany().HasForeignKey(s => s.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.SuppressedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
