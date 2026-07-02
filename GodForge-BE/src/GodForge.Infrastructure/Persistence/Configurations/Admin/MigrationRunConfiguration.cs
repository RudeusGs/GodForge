using GodForge.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Admin;

public sealed class MigrationRunConfiguration : IEntityTypeConfiguration<MigrationRun>
{
    public void Configure(EntityTypeBuilder<MigrationRun> builder)
    {
        builder.ToTable("migration_runs", "admin");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(r => r.MigrationName).HasColumnName("migration_name").HasMaxLength(200).IsRequired();
        builder.Property(r => r.MigrationVersion).HasColumnName("migration_version").HasMaxLength(100).IsRequired();
        builder.Property(r => r.Checksum).HasColumnName("checksum").HasMaxLength(120);
        builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(r => r.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        builder.Property(r => r.ExecutedBy).HasColumnName("executed_by").HasMaxLength(120);
        builder.Property(r => r.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
    }
}
