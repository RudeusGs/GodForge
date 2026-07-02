using GodForge.Domain.Entities.Storage;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Storage;

public sealed class ArtifactAccessLogConfiguration : IEntityTypeConfiguration<ArtifactAccessLog>
{
    public void Configure(EntityTypeBuilder<ArtifactAccessLog> builder)
    {
        builder.ToTable("artifact_access_logs", "storage");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(l => l.ArtifactId).HasColumnName("artifact_id").HasColumnType("uuid").IsRequired();
        builder.Property(l => l.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(l => l.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(l => l.Action).HasColumnName("action").HasMaxLength(40).IsRequired();
        builder.Property(l => l.AccessedAt).HasColumnName("accessed_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Artifact>().WithMany().HasForeignKey(l => l.ArtifactId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.ArtifactId, l.AccessedAt }).HasDatabaseName("ix_artifact_access_logs_artifact_time").IsDescending(false, true);
    }
}
