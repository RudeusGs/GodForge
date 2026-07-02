using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Repo;

public sealed class GitCommitConfiguration : IEntityTypeConfiguration<GitCommit>
{
    public void Configure(EntityTypeBuilder<GitCommit> builder)
    {
        builder.ToTable("git_commits", "repo");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(c => c.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(c => c.Hash).HasColumnName("hash").HasMaxLength(40).IsRequired();
        builder.Property(c => c.TreeHash).HasColumnName("tree_hash").HasMaxLength(40).IsRequired();
        builder.Property(c => c.AuthorName).HasColumnName("author_name").HasMaxLength(255).IsRequired();
        builder.Property(c => c.AuthorEmail).HasColumnName("author_email").HasMaxLength(255).IsRequired();
        builder.Property(c => c.Message).HasColumnName("message").HasColumnType("text").IsRequired();
        builder.Property(c => c.AuthoredAt).HasColumnName("authored_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(c => c.CommittedAt).HasColumnName("committed_at").HasColumnType("timestamptz").IsRequired();
        
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Repository>().WithMany().HasForeignKey(c => c.RepositoryId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.RepositoryId, c.Hash }).HasDatabaseName("ux_git_commits_repo_hash").IsUnique();
        builder.HasIndex(c => new { c.RepositoryId, c.CommittedAt }).HasDatabaseName("ix_git_commits_repo_time").IsDescending(false, true);
    }
}
