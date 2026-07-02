using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
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

        builder.Property(i => i.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(i => i.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
        builder.Property(i => i.InvitedBy).HasColumnName("invited_by").HasColumnType("uuid").IsRequired();
        builder.Property(i => i.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz").IsRequired();

        builder.Property(i => i.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(i => i.InvitedBy).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.TokenHash).HasDatabaseName("ux_user_invites_token").IsUnique();
        builder.HasIndex(i => new { i.Email, i.Status })
               .HasDatabaseName("ix_user_invites_email_status");
    }
}
