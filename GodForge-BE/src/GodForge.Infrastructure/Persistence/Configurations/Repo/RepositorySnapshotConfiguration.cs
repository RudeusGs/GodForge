using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Repo;

public sealed class RepositorySnapshotConfiguration : IEntityTypeConfiguration<RepositorySnapshot>
{
    public void Configure(EntityTypeBuilder<RepositorySnapshot> builder)
    {
        builder.ToTable("repository_snapshots", "repo");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(s => s.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.CommitHash).HasColumnName("commit_hash").HasMaxLength(40).IsRequired();
        builder.Property(s => s.BranchName).HasColumnName("branch_name").HasMaxLength(255).IsRequired();
        builder.Property(s => s.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(s => s.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Repository>().WithMany().HasForeignKey(s => s.RepositoryId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.RepositoryId, s.CommitHash }).HasDatabaseName("ux_repository_snapshots_repo_commit").IsUnique();
        builder.HasIndex(s => new { s.RepositoryId, s.CreatedAt }).HasDatabaseName("ix_repository_snapshots_repo_time").IsDescending(false, true);
    }
}
