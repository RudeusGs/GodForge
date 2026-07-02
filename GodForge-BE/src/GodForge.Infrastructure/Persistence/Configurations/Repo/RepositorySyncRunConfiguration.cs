using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Repo;

public sealed class RepositorySyncRunConfiguration : IEntityTypeConfiguration<RepositorySyncRun>
{
    public void Configure(EntityTypeBuilder<RepositorySyncRun> builder)
    {
        builder.ToTable("repository_sync_runs", "repo");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(r => r.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(r => r.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        builder.Property(r => r.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
        
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<GitRepository>().WithMany().HasForeignKey(r => r.RepositoryId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.RepositoryId, r.CreatedAt }).HasDatabaseName("ix_repository_sync_runs_repo_time").IsDescending(false, true);
        builder.HasIndex(r => new { r.RepositoryId, r.Status }).HasDatabaseName("ix_repository_sync_runs_status");
    }
}
