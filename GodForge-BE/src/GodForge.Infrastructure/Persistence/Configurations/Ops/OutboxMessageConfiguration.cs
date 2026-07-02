using GodForge.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Ops;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", "ops");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(o => o.AggregateType).HasColumnName("aggregate_type").HasMaxLength(100).IsRequired();
        builder.Property(o => o.AggregateId).HasColumnName("aggregate_id").HasColumnType("uuid");
        builder.Property(o => o.EventType).HasColumnName("event_type").HasConversion<string>().HasMaxLength(120).IsRequired();
        builder.Property(o => o.PayloadJson).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(o => o.HeadersJson).HasColumnName("headers").HasColumnType("jsonb");
        builder.Property(o => o.CorrelationId).HasColumnName("correlation_id").HasMaxLength(80).IsRequired();
        builder.Property(o => o.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(o => o.Attempts).HasColumnName("attempts").IsRequired();
        builder.Property(o => o.AvailableAt).HasColumnName("available_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(o => o.ProcessedAt).HasColumnName("processed_at").HasColumnType("timestamptz");
        builder.Property(o => o.ErrorMessage).HasColumnName("error_message").HasColumnType("text");

        builder.Property(o => o.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(o => new { o.Status, o.AvailableAt }).HasDatabaseName("ix_outbox_status_available");
    }
}
