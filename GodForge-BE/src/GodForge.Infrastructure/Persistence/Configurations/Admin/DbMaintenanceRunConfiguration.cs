using GodForge.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Admin;

public sealed class DbMaintenanceRunConfiguration : IEntityTypeConfiguration<DbMaintenanceRun>
{
    public void Configure(EntityTypeBuilder<DbMaintenanceRun> builder)
    {
        builder.ToTable("db_maintenance_runs", "admin");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(r => r.MaintenanceType).HasColumnName("maintenance_type").HasMaxLength(100).IsRequired();
        builder.Property(r => r.Target).HasColumnName("target").HasMaxLength(200);
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(r => r.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        builder.Property(r => r.DetailsJson).HasColumnName("details").HasColumnType("jsonb");
    }
}
