using GodForge.Domain.Entities.Analysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Analysis;

public sealed class HealthRuleVersionConfiguration : IEntityTypeConfiguration<HealthRuleVersion>
{
    public void Configure(EntityTypeBuilder<HealthRuleVersion> builder)
    {
        builder.ToTable("health_rule_versions", "analysis");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(v => v.RuleId).HasColumnName("rule_id").HasColumnType("uuid").IsRequired();
        builder.Property(v => v.Version).HasColumnName("version").IsRequired();
        builder.Property(v => v.ConfigJson).HasColumnName("config").HasColumnType("jsonb");
        
        builder.Property(v => v.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<HealthRule>().WithMany().HasForeignKey(v => v.RuleId).OnDelete(DeleteBehavior.Cascade);
    }
}
