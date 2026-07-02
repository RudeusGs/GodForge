using GodForge.Domain.Entities.Collab;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Collab;

public sealed class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("activities", "collab");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(a => a.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(a => a.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid");
        builder.Property(a => a.ActorId).HasColumnName("actor_id").HasColumnType("uuid");
        builder.Property(a => a.Action).HasColumnName("action").HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(a => a.TargetType).HasColumnName("target_type").HasMaxLength(100);
        builder.Property(a => a.TargetId).HasColumnName("target_id").HasMaxLength(100);
        builder.Property(a => a.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        builder.Property(a => a.CorrelationId).HasColumnName("correlation_id").HasMaxLength(80).IsRequired();

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Project>().WithMany().HasForeignKey(a => a.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<GitRepository>().WithMany().HasForeignKey(a => a.RepositoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.ActorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.ProjectId, a.CreatedAt }).HasDatabaseName("ix_activities_project_time").IsDescending(false, true);
        builder.HasIndex(a => a.RepositoryId).HasDatabaseName("ix_activities_repo");
        builder.HasIndex(a => a.ActorId).HasDatabaseName("ix_activities_actor");
    }
}
