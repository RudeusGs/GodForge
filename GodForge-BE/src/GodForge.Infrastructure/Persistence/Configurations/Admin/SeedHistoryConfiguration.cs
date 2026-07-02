using GodForge.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Admin;

public sealed class SeedHistoryConfiguration : IEntityTypeConfiguration<SeedHistory>
{
    public void Configure(EntityTypeBuilder<SeedHistory> builder)
    {
        builder.ToTable("seed_history", "admin");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(h => h.SeedName).HasColumnName("seed_name").HasMaxLength(200).IsRequired();
        builder.Property(h => h.Checksum).HasColumnName("checksum").HasMaxLength(120).IsRequired();
        builder.Property(h => h.AppliedAt).HasColumnName("applied_at").HasColumnType("timestamptz").IsRequired();
    }
}
