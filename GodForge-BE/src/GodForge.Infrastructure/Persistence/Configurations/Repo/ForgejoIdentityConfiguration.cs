using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Repo;

public sealed class ForgejoIdentityConfiguration : IEntityTypeConfiguration<ForgejoIdentity>
{
    public void Configure(EntityTypeBuilder<ForgejoIdentity> builder)
    {
        builder.ToTable("forgejo_identities", "repo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Login).HasMaxLength(100);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.ProviderUserId).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
