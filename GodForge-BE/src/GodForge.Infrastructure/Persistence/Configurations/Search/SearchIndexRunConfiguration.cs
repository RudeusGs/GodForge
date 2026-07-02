using GodForge.Domain.Entities.Search;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Search;

public sealed class SearchIndexRunConfiguration : IEntityTypeConfiguration<SearchIndexRun>
{
    public void Configure(EntityTypeBuilder<SearchIndexRun> builder)
    {
        builder.ToTable("search_index_runs", "search");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(r => r.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.SnapshotId).HasColumnName("snapshot_id").HasColumnType("uuid");
        builder.Property(r => r.JobId).HasColumnName("job_id").HasColumnType("uuid");
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(r => r.DocumentCount).HasColumnName("document_count").IsRequired();
        builder.Property(r => r.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");

        builder.HasOne<Project>().WithMany().HasForeignKey(r => r.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RepositorySnapshot>().WithMany().HasForeignKey(r => r.SnapshotId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Job>().WithMany().HasForeignKey(r => r.JobId).OnDelete(DeleteBehavior.SetNull);
    }
}
