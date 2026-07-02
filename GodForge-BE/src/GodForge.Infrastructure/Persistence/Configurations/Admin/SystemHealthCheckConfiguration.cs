using GodForge.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Admin;

public sealed class SystemHealthCheckConfiguration : IEntityTypeConfiguration<SystemHealthCheck>
{
    public void Configure(EntityTypeBuilder<SystemHealthCheck> builder)
    {
        builder.ToTable("system_health_checks", "admin");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(c => c.Component).HasColumnName("component").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(c => c.DetailsJson).HasColumnName("details").HasColumnType("jsonb");
        builder.Property(c => c.CheckedAt).HasColumnName("checked_at").HasColumnType("timestamptz").IsRequired();
    }
}
