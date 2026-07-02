using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Identity;

public sealed class EmailChangeRequestConfiguration : IEntityTypeConfiguration<EmailChangeRequest>
{
    public void Configure(EntityTypeBuilder<EmailChangeRequest> builder)
    {
        builder.ToTable("email_change_requests", "identity");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.NewEmail).HasColumnName("new_email").HasMaxLength(255).IsRequired();
        builder.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.TokenHash).HasDatabaseName("ux_email_change_requests_token").IsUnique();
        builder.HasIndex(e => new { e.UserId, e.ExpiresAt })
               .HasDatabaseName("ix_email_change_requests_user")
               .HasFilter("completed_at IS NULL");
    }
}
