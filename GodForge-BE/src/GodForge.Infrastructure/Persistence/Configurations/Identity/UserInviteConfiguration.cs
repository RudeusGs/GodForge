using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserInviteConfiguration : IEntityTypeConfiguration<UserInvite>
{
    public void Configure(EntityTypeBuilder<UserInvite> builder)
    {
        builder.ToTable("user_invites", "identity");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(i => i.OrganizationId).HasColumnName("organization_id").HasColumnType("uuid").IsRequired();
        builder.Property(i => i.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        builder.Property(i => i.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(320).IsRequired();
        builder.Property(i => i.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(i => i.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
        builder.Property(i => i.InvitedBy).HasColumnName("invited_by").HasColumnType("uuid").IsRequired();
        builder.Property(i => i.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(i => i.AcceptedAt).HasColumnName("accepted_at").HasColumnType("timestamptz");
        builder.Property(i => i.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");
        builder.Property(i => i.Version).HasColumnName("version").IsRequired();
        builder.Property(i => i.ConcurrencyStamp).HasColumnName("concurrency_stamp").HasMaxLength(64).IsRequired().IsConcurrencyToken();
        builder.Property(i => i.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
        builder.HasOne<Organization>().WithMany().HasForeignKey(i => i.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(i => i.InvitedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.TokenHash).HasDatabaseName("ux_user_invites_token").IsUnique();
        builder.HasIndex(i => new { i.OrganizationId, i.NormalizedEmail, i.Status })
            .HasDatabaseName("ix_user_invites_org_email_status");
        builder.HasIndex(i => new { i.OrganizationId, i.NormalizedEmail })
            .HasDatabaseName("ux_user_invites_active_org_email")
            .IsUnique()
            .HasFilter("status = 'Pending'");
    }
}
