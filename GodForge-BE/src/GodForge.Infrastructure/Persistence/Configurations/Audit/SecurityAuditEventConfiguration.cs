using GodForge.Domain.Entities.Audit;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Audit;

public sealed class SecurityAuditEventConfiguration : IEntityTypeConfiguration<SecurityAuditEvent>
{
    public void Configure(EntityTypeBuilder<SecurityAuditEvent> builder)
    {
        builder.ToTable("security_audit_events", "audit");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid");
        builder.Property(e => e.EventType).HasColumnName("event_type").HasConversion<string>().HasMaxLength(120).IsRequired();
        builder.Property(e => e.Severity).HasColumnName("severity").HasMaxLength(30).IsRequired();
        builder.Property(e => e.DetailsJson).HasColumnName("details").HasColumnType("jsonb");
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(80).IsRequired();
        
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}
