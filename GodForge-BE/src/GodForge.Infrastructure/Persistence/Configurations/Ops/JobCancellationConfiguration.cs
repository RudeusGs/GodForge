using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Ops;

public sealed class JobCancellationConfiguration : IEntityTypeConfiguration<JobCancellation>
{
    public void Configure(EntityTypeBuilder<JobCancellation> builder)
    {
        builder.ToTable("job_cancellations", "ops");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(c => c.JobId).HasColumnName("job_id").HasColumnType("uuid").IsRequired();
        builder.Property(c => c.RequestedBy).HasColumnName("requested_by").HasColumnType("uuid").IsRequired();
        builder.Property(c => c.Reason).HasColumnName("reason").HasColumnType("text");

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Job>().WithMany().HasForeignKey(c => c.JobId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.RequestedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
