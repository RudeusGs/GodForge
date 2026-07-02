using GodForge.Domain.Entities.Collab;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Collab;

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments", "collab");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(c => c.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(c => c.AuthorId).HasColumnName("author_id").HasColumnType("uuid").IsRequired();
        builder.Property(c => c.TargetType).HasColumnName("target_type").HasMaxLength(100).IsRequired();
        builder.Property(c => c.TargetId).HasColumnName("target_id").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Content).HasColumnName("content").HasColumnType("text").IsRequired();
        builder.Property(c => c.ParentId).HasColumnName("parent_id").HasColumnType("uuid");
        builder.Property(c => c.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasOne<Project>().WithMany().HasForeignKey(c => c.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.TargetType, c.TargetId }).HasDatabaseName("ix_comments_target");
        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}
