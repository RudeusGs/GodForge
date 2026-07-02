using GodForge.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Ops;

public sealed class JobDependencyConfiguration : IEntityTypeConfiguration<JobDependency>
{
    public void Configure(EntityTypeBuilder<JobDependency> builder)
    {
        builder.ToTable("job_dependencies", "ops");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(d => d.JobId).HasColumnName("job_id").HasColumnType("uuid").IsRequired();
        builder.Property(d => d.DependsOnJobId).HasColumnName("depends_on_job_id").HasColumnType("uuid").IsRequired();
        
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Job>().WithMany().HasForeignKey(d => d.JobId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Job>().WithMany().HasForeignKey(d => d.DependsOnJobId).OnDelete(DeleteBehavior.Restrict);
    }
}
