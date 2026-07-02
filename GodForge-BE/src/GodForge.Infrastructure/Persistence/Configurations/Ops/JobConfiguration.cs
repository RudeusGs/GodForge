using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Ops;

public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs", "ops");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(j => j.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(j => j.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid");
        builder.Property(j => j.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(j => j.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(j => j.QueueName).HasColumnName("queue_name").HasMaxLength(100).IsRequired();
        builder.Property(j => j.Priority).HasColumnName("priority").IsRequired();
        builder.Property(j => j.Progress).HasColumnName("progress").IsRequired();
        builder.Property(j => j.Payload).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(j => j.Result).HasColumnName("result").HasColumnType("jsonb");
        builder.Property(j => j.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(j => j.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(160);
        builder.Property(j => j.MaxAttempts).HasColumnName("max_attempts").HasDefaultValue(3).IsRequired();
        builder.Property(j => j.AttemptCount).HasColumnName("attempt_count").IsRequired();
        
        builder.Property(j => j.AvailableAt).HasColumnName("available_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(j => j.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz");
        builder.Property(j => j.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        builder.Property(j => j.CancelledAt).HasColumnName("cancelled_at").HasColumnType("timestamptz");
        builder.Property(j => j.TimeoutAt).HasColumnName("timeout_at").HasColumnType("timestamptz");
        builder.Property(j => j.LastHeartbeatAt).HasColumnName("last_heartbeat_at").HasColumnType("timestamptz");
        builder.Property(j => j.CancellationRequestedAt).HasColumnName("cancellation_requested_at").HasColumnType("timestamptz");
        
        builder.Property(j => j.ErrorCode).HasColumnName("error_code").HasMaxLength(100);
        builder.Property(j => j.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
        builder.Property(j => j.TriggeredBy).HasColumnName("triggered_by").HasColumnType("uuid");
        builder.Property(j => j.CorrelationId).HasColumnName("correlation_id").HasMaxLength(80).IsRequired();
        
        builder.Property(j => j.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(j => j.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Project>().WithMany().HasForeignKey(j => j.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<GitRepository>().WithMany().HasForeignKey(j => j.RepositoryId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(j => j.TriggeredBy).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(j => new { j.Status, j.AvailableAt, j.Priority, j.CreatedAt })
               .HasDatabaseName("ix_jobs_queue_poll")
               .IsDescending(false, false, true, false);

        builder.HasIndex(j => new { j.ProjectId, j.CreatedAt })
               .HasDatabaseName("ix_jobs_project_created")
               .IsDescending(false, true);

        builder.HasIndex(j => new { j.Type, j.Status })
               .HasDatabaseName("ix_jobs_type_status");

        builder.HasIndex(j => new { j.ProjectId, j.Type, j.IdempotencyKey })
               .HasDatabaseName("ux_jobs_idempotency")
               .IsUnique()
               .HasFilter("idempotency_key IS NOT NULL");
    }
}
