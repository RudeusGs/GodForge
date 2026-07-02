using GodForge.Domain.Entities.Collab;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Collab;

public sealed class ReviewThreadCommentConfiguration : IEntityTypeConfiguration<ReviewThreadComment>
{
    public void Configure(EntityTypeBuilder<ReviewThreadComment> builder)
    {
        builder.ToTable("review_thread_comments", "collab");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(c => c.ThreadId).HasColumnName("thread_id").HasColumnType("uuid").IsRequired();
        builder.Property(c => c.AuthorId).HasColumnName("author_id").HasColumnType("uuid").IsRequired();
        builder.Property(c => c.Content).HasColumnName("content").HasColumnType("text").IsRequired();

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<ReviewThread>().WithMany().HasForeignKey(c => c.ThreadId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.ThreadId, c.CreatedAt }).HasDatabaseName("ix_review_thread_comments_thread_time").IsDescending(false, true);
    }
}
