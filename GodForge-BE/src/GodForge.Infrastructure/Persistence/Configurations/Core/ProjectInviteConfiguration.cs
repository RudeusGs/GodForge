using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Core;

public sealed class ProjectInviteConfiguration : IEntityTypeConfiguration<ProjectInvite>
{
    public void Configure(EntityTypeBuilder<ProjectInvite> builder)
    {
        builder.ToTable("project_invites", "core");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(i => i.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(i => i.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(i => i.Role).HasColumnName("role").HasMaxLength(40).IsRequired();
        builder.Property(i => i.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
        builder.Property(i => i.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.InvitedBy).HasColumnName("invited_by").HasColumnType("uuid").IsRequired();
        builder.Property(i => i.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(i => i.AcceptedAt).HasColumnName("accepted_at").HasColumnType("timestamptz");
        
        builder.Property(i => i.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Project>().WithMany().HasForeignKey(i => i.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(i => i.InvitedBy).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.TokenHash).HasDatabaseName("ux_project_invites_token").IsUnique();
        builder.HasIndex(i => new { i.ProjectId, i.Email, i.Status })
               .HasDatabaseName("ix_project_invites_project_email_status");
    }
}
