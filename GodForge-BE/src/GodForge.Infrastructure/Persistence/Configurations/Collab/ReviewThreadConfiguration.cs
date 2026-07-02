using GodForge.Domain.Entities.Collab;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Collab;

public sealed class ReviewThreadConfiguration : IEntityTypeConfiguration<ReviewThread>
{
    public void Configure(EntityTypeBuilder<ReviewThread> builder)
    {
        builder.ToTable("review_threads", "collab");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(t => t.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(t => t.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(t => t.TargetId).HasColumnName("target_id").HasMaxLength(100).IsRequired();
        builder.Property(t => t.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(t => t.CreatedBy).HasColumnName("created_by").HasColumnType("uuid").IsRequired();
        
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Project>().WithMany().HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Repository>().WithMany().HasForeignKey(t => t.RepositoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(t => t.CreatedBy).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.RepositoryId, t.TargetId }).HasDatabaseName("ix_review_threads_target");
    }
}
