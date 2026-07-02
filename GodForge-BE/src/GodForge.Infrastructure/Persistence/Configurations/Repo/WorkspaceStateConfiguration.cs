using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Repo;

public sealed class WorkspaceStateConfiguration : IEntityTypeConfiguration<WorkspaceState>
{
    public void Configure(EntityTypeBuilder<WorkspaceState> builder)
    {
        builder.ToTable("workspace_states", "repo");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(s => s.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.HeadCommit).HasColumnName("head_commit").HasMaxLength(40).IsRequired();
        builder.Property(s => s.BranchName).HasColumnName("branch_name").HasMaxLength(255).IsRequired();
        builder.Property(s => s.IsLocked).HasColumnName("is_locked").IsRequired();
        builder.Property(s => s.LockedBy).HasColumnName("locked_by").HasMaxLength(120);
        builder.Property(s => s.LockedAt).HasColumnName("locked_at").HasColumnType("timestamptz");
        
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<GitRepository>().WithOne().HasForeignKey<WorkspaceState>(s => s.RepositoryId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.RepositoryId).HasDatabaseName("ux_workspace_states_repo").IsUnique();
    }
}
