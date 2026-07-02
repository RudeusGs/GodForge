using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Repo;

public sealed class RepositoryFileConfiguration : IEntityTypeConfiguration<RepositoryFile>
{
    public void Configure(EntityTypeBuilder<RepositoryFile> builder)
    {
        builder.ToTable("repository_files", "repo");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(f => f.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(f => f.Path).HasColumnName("path").HasColumnType("text").IsRequired();
        builder.Property(f => f.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(f => f.Size).HasColumnName("size").HasColumnType("bigint").IsRequired();
        builder.Property(f => f.LastCommitHash).HasColumnName("last_commit_hash").HasMaxLength(40).IsRequired();

        builder.Property(f => f.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<GitRepository>().WithMany().HasForeignKey(f => f.RepositoryId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.RepositoryId, f.Path }).HasDatabaseName("ux_repository_files_repo_path").IsUnique();
        builder.HasIndex(f => new { f.RepositoryId, f.LastCommitHash }).HasDatabaseName("ix_repository_files_repo_commit");
    }
}
