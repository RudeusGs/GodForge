using GodForge.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Admin;

public sealed class AppVersionConfiguration : IEntityTypeConfiguration<AppVersion>
{
    public void Configure(EntityTypeBuilder<AppVersion> builder)
    {
        builder.ToTable("app_versions", "admin");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(v => v.ServiceName).HasColumnName("service_name").HasMaxLength(100).IsRequired();
        builder.Property(v => v.Version).HasColumnName("version").HasMaxLength(80).IsRequired();
        builder.Property(v => v.GitSha).HasColumnName("git_sha").HasMaxLength(80);
        builder.Property(v => v.DeployedAt).HasColumnName("deployed_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(v => v.DeployedBy).HasColumnName("deployed_by").HasMaxLength(120);
    }
}
