using GodForge.Domain.Entities.Governance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Governance;

public sealed class RetentionPolicyConfiguration : IEntityTypeConfiguration<RetentionPolicy>
{
    public void Configure(EntityTypeBuilder<RetentionPolicy> builder)
    {
        builder.ToTable("retention_policies", "governance");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(p => p.Scope).HasColumnName("scope").HasMaxLength(40).IsRequired();
        builder.Property(p => p.Target).HasColumnName("target").HasMaxLength(120).IsRequired();
        builder.Property(p => p.RetentionDays).HasColumnName("retention_days").IsRequired();
        builder.Property(p => p.Action).HasColumnName("action").HasMaxLength(30).IsRequired();
        builder.Property(p => p.IsEnabled).HasColumnName("is_enabled").IsRequired();
        
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
    }
}
