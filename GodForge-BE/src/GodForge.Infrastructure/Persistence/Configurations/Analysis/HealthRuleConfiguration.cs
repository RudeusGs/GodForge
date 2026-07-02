using GodForge.Domain.Entities.Analysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Analysis;

public sealed class HealthRuleConfiguration : IEntityTypeConfiguration<HealthRule>
{
    public void Configure(EntityTypeBuilder<HealthRule> builder)
    {
        builder.ToTable("health_rules", "analysis");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(r => r.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(r => r.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(r => r.DefaultSeverity).HasColumnName("default_severity").HasMaxLength(20).IsRequired();
        builder.Property(r => r.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(r => r.ConfigJson).HasColumnName("config").HasColumnType("jsonb");
        
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(r => r.Code).HasDatabaseName("ux_health_rules_code").IsUnique();
    }
}
