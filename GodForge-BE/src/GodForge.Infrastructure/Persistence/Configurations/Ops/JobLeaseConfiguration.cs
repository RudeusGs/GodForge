using GodForge.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Ops;

public sealed class JobLeaseConfiguration : IEntityTypeConfiguration<JobLease>
{
    public void Configure(EntityTypeBuilder<JobLease> builder)
    {
        builder.ToTable("job_leases", "ops");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(l => l.JobId).HasColumnName("job_id").HasColumnType("uuid").IsRequired();
        builder.Property(l => l.WorkerInstanceId).HasColumnName("worker_instance_id").HasMaxLength(120).IsRequired();
        builder.Property(l => l.LeaseToken).HasColumnName("lease_token").HasMaxLength(120).IsRequired();
        builder.Property(l => l.LeasedUntil).HasColumnName("leased_until").HasColumnType("timestamptz").IsRequired();
        builder.Property(l => l.AcquiredAt).HasColumnName("acquired_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(l => l.RenewedAt).HasColumnName("renewed_at").HasColumnType("timestamptz");
        builder.Property(l => l.ReleasedAt).HasColumnName("released_at").HasColumnType("timestamptz");

        builder.HasOne<Job>().WithMany().HasForeignKey(l => l.JobId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => l.LeasedUntil).HasDatabaseName("ix_job_leases_expired");
        builder.HasIndex(l => l.JobId)
               .HasDatabaseName("ux_job_leases_active")
               .IsUnique()
               .HasFilter("released_at IS NULL");
    }
}
