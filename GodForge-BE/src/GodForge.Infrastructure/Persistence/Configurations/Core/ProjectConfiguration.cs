using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Core;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects", "core");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(p => p.OrganizationId).HasColumnName("organization_id").HasColumnType("uuid").IsRequired();

        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
        builder.Property(p => p.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(p => p.GodotVersion).HasColumnName("godot_version").HasMaxLength(30).IsRequired();
        builder.Property(p => p.Visibility).HasColumnName("visibility").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(p => p.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(p => p.HealthScore).HasColumnName("health_score");
        builder.Property(p => p.CreatedBy).HasColumnName("created_by").HasColumnType("uuid").IsRequired();
        builder.Property(p => p.Version).HasColumnName("version").IsConcurrencyToken().IsRequired();

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(p => p.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamptz");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasOne<User>().WithMany().HasForeignKey(p => p.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Organization>().WithMany().HasForeignKey(p => p.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(p => new { p.Id, p.OrganizationId });

        builder.HasIndex(p => new { p.OrganizationId, p.Slug }).HasDatabaseName("ux_projects_org_slug_active").IsUnique().HasFilter("deleted_at IS NULL");
        builder.HasIndex(p => p.Status).HasDatabaseName("ix_projects_status");
        builder.HasIndex(p => p.CreatedBy).HasDatabaseName("ix_projects_created_by");
        builder.HasIndex(p => new { p.OrganizationId, p.Status, p.CreatedAt }).HasDatabaseName("ix_projects_org_status_created");
        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
