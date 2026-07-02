using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Repo;

public sealed class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("webhook_events", "repo");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(e => e.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.Provider).HasColumnName("provider").HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(e => e.EventType).HasColumnName("event_type").HasConversion<string>().HasMaxLength(120).IsRequired();
        builder.Property(e => e.DeliveryId).HasColumnName("delivery_id").HasMaxLength(120).IsRequired();
        builder.Property(e => e.PayloadJson).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.ProcessedAt).HasColumnName("processed_at").HasColumnType("timestamptz");
        
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<GitRepository>().WithMany().HasForeignKey(e => e.RepositoryId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.Provider, e.DeliveryId }).HasDatabaseName("ux_webhook_events_provider_delivery").IsUnique();
    }
}
