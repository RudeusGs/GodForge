using GodForge.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Core;

public sealed class ProjectSettingConfiguration : IEntityTypeConfiguration<ProjectSetting>
{
    public void Configure(EntityTypeBuilder<ProjectSetting> builder)
    {
        builder.ToTable("project_settings", "core");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(s => s.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.DefaultRole).HasColumnName("default_role").HasMaxLength(40).IsRequired();
        builder.Property(s => s.Visibility).HasColumnName("visibility").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.FeaturesJson).HasColumnName("features").HasColumnType("jsonb");
        
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Project>().WithOne().HasForeignKey<ProjectSetting>(s => s.ProjectId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.ProjectId).HasDatabaseName("ux_project_settings_project").IsUnique();
    }
}
