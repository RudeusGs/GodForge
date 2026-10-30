using GodForge.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Core;

public sealed class OrganizationSettingConfiguration : IEntityTypeConfiguration<OrganizationSetting>
{
    public void Configure(EntityTypeBuilder<OrganizationSetting> builder)
    {
        builder.ToTable("organization_settings", "core", t => t.HasCheckConstraint("ck_organization_settings_version", "version > 0"));
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Version).HasDefaultValue(1L).IsConcurrencyToken();
        builder.Property(s => s.AiAdvisoryAllowed).HasDefaultValue(true);
        builder.HasIndex(s => s.OrganizationId).IsUnique();
        builder.HasOne<Organization>().WithOne().HasForeignKey<OrganizationSetting>(s => s.OrganizationId).OnDelete(DeleteBehavior.Restrict);
    }
}
