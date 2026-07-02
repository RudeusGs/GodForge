using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Storage;

public sealed class SceneDiffConfiguration : IEntityTypeConfiguration<SceneDiff>
{
    public void Configure(EntityTypeBuilder<SceneDiff> builder)
    {
        builder.ToTable("scene_diffs", "storage");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(d => d.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(d => d.BaseCommit).HasColumnName("base_commit").HasMaxLength(40).IsRequired();
        builder.Property(d => d.HeadCommit).HasColumnName("head_commit").HasMaxLength(40).IsRequired();
        builder.Property(d => d.ScenePath).HasColumnName("scene_path").HasMaxLength(500).IsRequired();
        builder.Property(d => d.DiffJsonPath).HasColumnName("diff_json_path").HasMaxLength(500);
        builder.Property(d => d.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<GitRepository>().WithMany().HasForeignKey(d => d.RepositoryId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.RepositoryId, d.BaseCommit, d.HeadCommit, d.ScenePath }).HasDatabaseName("ux_scene_diffs_repo_commits_path").IsUnique();
    }
}
