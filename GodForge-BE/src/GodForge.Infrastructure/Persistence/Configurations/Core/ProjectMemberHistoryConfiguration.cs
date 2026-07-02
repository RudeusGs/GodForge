using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Core;

public sealed class ProjectMemberHistoryConfiguration : IEntityTypeConfiguration<ProjectMemberHistory>
{
    public void Configure(EntityTypeBuilder<ProjectMemberHistory> builder)
    {
        builder.ToTable("project_member_history", "core");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(h => h.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(h => h.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(h => h.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(h => h.Action).HasColumnName("action").HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(h => h.ActorId).HasColumnName("actor_id").HasColumnType("uuid").IsRequired();
        
        builder.Property(h => h.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Project>().WithMany().HasForeignKey(h => h.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.ProjectId, h.CreatedAt }).HasDatabaseName("ix_project_member_history_project").IsDescending(false, true);
    }
}
