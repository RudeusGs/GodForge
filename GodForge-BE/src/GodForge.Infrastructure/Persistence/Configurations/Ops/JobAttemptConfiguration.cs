using GodForge.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Ops;

public sealed class JobAttemptConfiguration : IEntityTypeConfiguration<JobAttempt>
{
    public void Configure(EntityTypeBuilder<JobAttempt> builder)
    {
        builder.ToTable("job_attempts", "ops");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(a => a.JobId).HasColumnName("job_id").HasColumnType("uuid").IsRequired();
        builder.Property(a => a.AttemptNumber).HasColumnName("attempt_number").IsRequired();
        builder.Property(a => a.WorkerName).HasColumnName("worker_name").HasMaxLength(100);
        builder.Property(a => a.WorkerInstanceId).HasColumnName("worker_instance_id").HasMaxLength(120);
        builder.Property(a => a.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(a => a.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(a => a.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        builder.Property(a => a.DurationMs).HasColumnName("duration_ms");
        builder.Property(a => a.ErrorCode).HasColumnName("error_code").HasMaxLength(100);
        builder.Property(a => a.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
        builder.Property(a => a.StackTraceHash).HasColumnName("stack_trace_hash").HasMaxLength(100);

        builder.HasOne<Job>().WithMany().HasForeignKey(a => a.JobId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.JobId, a.AttemptNumber }).HasDatabaseName("ux_job_attempts_job_number").IsUnique();
    }
}
