using GodForge.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Ops;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages", "ops");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(i => i.MessageId).HasColumnName("message_id").HasMaxLength(160).IsRequired();
        builder.Property(i => i.ConsumerName).HasColumnName("consumer_name").HasMaxLength(120).IsRequired();
        builder.Property(i => i.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(i => i.ReceivedAt).HasColumnName("received_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(i => i.ProcessedAt).HasColumnName("processed_at").HasColumnType("timestamptz");
        builder.Property(i => i.ErrorMessage).HasColumnName("error_message").HasColumnType("text");

        builder.HasIndex(i => new { i.MessageId, i.ConsumerName }).HasDatabaseName("ux_inbox_message_consumer").IsUnique();
    }
}
