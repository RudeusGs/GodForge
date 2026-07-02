using GodForge.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Ops;

public sealed class JobEventConfiguration : IEntityTypeConfiguration<JobEvent>
{
    public void Configure(EntityTypeBuilder<JobEvent> builder)
    {
        builder.ToTable("job_events", "ops");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(e => e.JobId).HasColumnName("job_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.EventType).HasColumnName("event_type").HasConversion<string>().HasMaxLength(80).IsRequired();
        builder.Property(e => e.Progress).HasColumnName("progress");
        builder.Property(e => e.Message).HasColumnName("message").HasColumnType("text");
        builder.Property(e => e.DataJson).HasColumnName("data").HasColumnType("jsonb");
        
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Job>().WithMany().HasForeignKey(e => e.JobId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.JobId, e.CreatedAt }).HasDatabaseName("ix_job_events_job_created");
    }
}
