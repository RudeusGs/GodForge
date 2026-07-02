using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Core;

public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_members", "core");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(m => m.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(m => m.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(m => m.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(m => m.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(m => m.CreatedBy).HasColumnName("created_by").HasColumnType("uuid");
        builder.Property(m => m.JoinedAt).HasColumnName("joined_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(m => m.RemovedAt).HasColumnName("removed_at").HasColumnType("timestamptz");

        builder.Property(m => m.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Project>().WithMany().HasForeignKey(m => m.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ProjectId, x.UserId }).HasDatabaseName("ux_project_members_active").IsUnique().HasFilter("removed_at IS NULL");
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_project_members_user");
        builder.HasIndex(x => new { x.ProjectId, x.Role }).HasDatabaseName("ix_project_members_project_role");
    }
}
