using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Repo;

public class GitCommitFileConfiguration : IEntityTypeConfiguration<GitCommitFile>
{
    public void Configure(EntityTypeBuilder<GitCommitFile> builder)
    {
        builder.ToTable("git_commit_files", "repo");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.CommitId)
            .HasDatabaseName("ix_git_commit_files_commit");
        builder.HasIndex(x => x.FilePath)
            .HasDatabaseName("ix_git_commit_files_path");
    }
}
