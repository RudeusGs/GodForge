using GodForge.Domain.Entities.Audit;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Audit;

public sealed class DataAccessLogConfiguration : IEntityTypeConfiguration<DataAccessLog>
{
    public void Configure(EntityTypeBuilder<DataAccessLog> builder)
    {
        builder.ToTable("data_access_logs", "audit");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(l => l.UserId).HasColumnName("user_id").HasColumnType("uuid");
        builder.Property(l => l.ResourceType).HasColumnName("resource_type").HasMaxLength(100).IsRequired();
        builder.Property(l => l.ResourceId).HasColumnName("resource_id").HasColumnType("uuid");
        builder.Property(l => l.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
        builder.Property(l => l.Outcome).HasColumnName("outcome").HasMaxLength(30).IsRequired();
        builder.Property(l => l.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(l => l.CorrelationId).HasColumnName("correlation_id").HasMaxLength(80).IsRequired();
        
        builder.Property(l => l.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}
