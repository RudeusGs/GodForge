using GodForge.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Ops;

public sealed class WorkerHeartbeatConfiguration : IEntityTypeConfiguration<WorkerHeartbeat>
{
    public void Configure(EntityTypeBuilder<WorkerHeartbeat> builder)
    {
        builder.ToTable("worker_heartbeats", "ops");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(w => w.WorkerName).HasColumnName("worker_name").HasMaxLength(100).IsRequired();
        builder.Property(w => w.WorkerInstanceId).HasColumnName("worker_instance_id").HasMaxLength(120).IsRequired();
        // Postgres text[] array mapping requires Npgsql integration
        builder.Property(w => w.Queues).HasColumnName("queues").HasColumnType("text[]").IsRequired();
        builder.Property(w => w.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(w => w.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(w => w.LastSeenAt).HasColumnName("last_seen_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(w => w.MetadataJson).HasColumnName("metadata").HasColumnType("jsonb");

        builder.HasIndex(w => w.WorkerInstanceId).HasDatabaseName("ux_worker_heartbeats_instance").IsUnique();
    }
}
