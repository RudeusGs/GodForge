using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Repo;

public sealed class GitRepositoryConfiguration : IEntityTypeConfiguration<GitRepository>
{
    public void Configure(EntityTypeBuilder<GitRepository> builder)
    {
        builder.ToTable("repositories", "repo");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(r => r.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.Mode).HasColumnName("mode").HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(r => r.Provider).HasColumnName("provider").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(r => r.RemoteUrl).HasColumnName("remote_url").HasMaxLength(800).IsRequired();
        builder.Property(r => r.CloneUrlSanitized).HasColumnName("clone_url_sanitized").HasMaxLength(800).IsRequired();
        builder.Property(r => r.DefaultBranch).HasColumnName("default_branch").HasMaxLength(150).IsRequired();
        builder.Property(r => r.ExternalRepositoryId).HasColumnName("external_repository_id").HasMaxLength(200);
        builder.Property(r => r.HostedRepositoryId).HasColumnName("hosted_repository_id").HasMaxLength(200);
        builder.Property(r => r.WorkspaceKey).HasColumnName("workspace_key").HasMaxLength(200);
        builder.Property(r => r.GitRepositoryStatus).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(r => r.AutoAnalyzeEnabled).HasColumnName("auto_analyze_enabled").IsRequired();
        builder.Property(r => r.LastSyncedAt).HasColumnName("last_synced_at").HasColumnType("timestamptz");
        builder.Property(r => r.RepoSizeBytes).HasColumnName("repo_size_bytes").HasColumnType("bigint");
        builder.Property(r => r.CurrentCommitHash).HasColumnName("current_commit_hash").HasMaxLength(80);
        builder.Property(r => r.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(100);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.ProjectId)
            .HasDatabaseName("ux_repositories_project")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(r => new { r.Provider, r.ExternalRepositoryId })
            .HasDatabaseName("ix_repositories_provider_external_id");

        builder.HasIndex(r => new { r.ProjectId, r.GitRepositoryStatus })
            .HasDatabaseName("ix_repositories_project_status");

        builder.HasQueryFilter(r => r.DeletedAt == null);
    }
}
