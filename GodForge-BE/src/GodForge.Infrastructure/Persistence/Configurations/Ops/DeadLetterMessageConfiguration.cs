using GodForge.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Ops;

public sealed class DeadLetterMessageConfiguration : IEntityTypeConfiguration<DeadLetterMessage>
{
    public void Configure(EntityTypeBuilder<DeadLetterMessage> builder)
    {
        builder.ToTable("dead_letter_messages", "ops");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(d => d.QueueName).HasColumnName("queue_name").HasMaxLength(100).IsRequired();
        builder.Property(d => d.MessageId).HasColumnName("message_id").HasMaxLength(160).IsRequired();
        builder.Property(d => d.PayloadJson).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(d => d.Reason).HasColumnName("reason").HasColumnType("text").IsRequired();
        builder.Property(d => d.ErrorDetailsJson).HasColumnName("error_details").HasColumnType("jsonb");

        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(d => new { d.QueueName, d.CreatedAt }).HasDatabaseName("ix_dead_letter_messages_queue_time").IsDescending(false, true);
    }
}
