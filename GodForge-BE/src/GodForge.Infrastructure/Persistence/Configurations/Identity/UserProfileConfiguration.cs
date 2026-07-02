using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles", "identity");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(p => p.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(p => p.Bio).HasColumnName("bio").HasColumnType("text");
        builder.Property(p => p.Timezone).HasColumnName("timezone").HasMaxLength(80);
        builder.Property(p => p.Locale).HasColumnName("locale").HasMaxLength(20);
        builder.Property(p => p.CompanyName).HasColumnName("company_name").HasMaxLength(200);

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<User>().WithOne().HasForeignKey<UserProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.UserId).HasDatabaseName("ux_user_profiles_user_id").IsUnique();
    }
}
