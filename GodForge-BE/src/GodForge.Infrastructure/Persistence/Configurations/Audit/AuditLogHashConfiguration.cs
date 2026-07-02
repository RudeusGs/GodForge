using GodForge.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Audit;

public sealed class AuditLogHashConfiguration : IEntityTypeConfiguration<AuditLogHash>
{
    public void Configure(EntityTypeBuilder<AuditLogHash> builder)
    {
        builder.ToTable("audit_log_hashes", "audit");

        builder.HasKey(h => h.AuditLogId);
        builder.Property(h => h.AuditLogId).HasColumnName("audit_log_id").HasColumnType("uuid");

        builder.Property(h => h.PreviousHash).HasColumnName("previous_hash").HasMaxLength(128);
        builder.Property(h => h.CurrentHash).HasColumnName("current_hash").HasMaxLength(128).IsRequired();
        builder.Property(h => h.Algorithm).HasColumnName("algorithm").HasMaxLength(30).IsRequired();
        
        builder.Property(h => h.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<AuditLog>().WithOne().HasForeignKey<AuditLogHash>(h => h.AuditLogId).OnDelete(DeleteBehavior.Restrict);
    }
}
