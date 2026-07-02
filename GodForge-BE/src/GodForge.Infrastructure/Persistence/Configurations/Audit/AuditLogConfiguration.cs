using GodForge.Domain.Entities.Audit;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Audit;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", "audit");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(l => l.ProjectId).HasColumnName("project_id").HasColumnType("uuid");
        builder.Property(l => l.ActorUserId).HasColumnName("actor_user_id").HasColumnType("uuid");
        builder.Property(l => l.EventType).HasColumnName("event_type").HasMaxLength(120).IsRequired();
        builder.Property(l => l.ResourceType).HasColumnName("resource_type").HasMaxLength(100);
        builder.Property(l => l.ResourceId).HasColumnName("resource_id").HasColumnType("uuid");
        builder.Property(l => l.Outcome).HasColumnName("outcome").HasMaxLength(30).IsRequired();
        builder.Property(l => l.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(l => l.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(l => l.CorrelationId).HasColumnName("correlation_id").HasMaxLength(80).IsRequired();
        builder.Property(l => l.DetailsJson).HasColumnName("details").HasColumnType("jsonb");
        
        builder.Property(l => l.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Project>().WithMany().HasForeignKey(l => l.ProjectId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(l => l.ActorUserId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => new { l.ActorUserId, l.CreatedAt }).HasDatabaseName("ix_audit_logs_actor_created").IsDescending(false, true);
        builder.HasIndex(l => l.CorrelationId).HasDatabaseName("ix_audit_logs_correlation");
    }
}
