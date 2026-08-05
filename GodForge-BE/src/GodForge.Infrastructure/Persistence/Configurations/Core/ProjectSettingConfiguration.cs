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
        builder.Property(s => s.AnalysisProfileKey).HasColumnName("analysis_profile_key").HasMaxLength(80).IsRequired();
        builder.Property(s => s.AiAdvisoryEnabled).HasColumnName("ai_advisory_enabled").IsRequired();
        builder.Property(s => s.DefaultAssetVisibility).HasColumnName("default_asset_visibility").HasMaxLength(32).IsRequired();
        builder.Property(s => s.NotificationPolicyVersion).HasColumnName("notification_policy_version").IsRequired();
        builder.Property(s => s.Version).HasColumnName("version").IsConcurrencyToken().IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
        builder.HasOne<Project>().WithOne().HasForeignKey<ProjectSetting>(s => s.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => s.ProjectId).HasDatabaseName("ux_project_settings_project").IsUnique();
    }
}
