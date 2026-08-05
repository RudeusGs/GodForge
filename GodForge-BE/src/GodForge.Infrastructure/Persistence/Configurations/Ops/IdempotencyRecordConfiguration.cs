using GodForge.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Ops;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records", "ops");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.Operation).HasColumnName("operation").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(x => x.RequestHash).HasColumnName("request_hash").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.HasIndex(x => new { x.ActorUserId, x.Operation, x.Key })
            .HasDatabaseName("ux_idempotency_records_scope")
            .IsUnique();
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_idempotency_records_created");
    }
}
