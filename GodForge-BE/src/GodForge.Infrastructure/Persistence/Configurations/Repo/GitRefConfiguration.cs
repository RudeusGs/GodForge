using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Repo;

public sealed class GitRefConfiguration : IEntityTypeConfiguration<GitRef>
{
    public void Configure(EntityTypeBuilder<GitRef> builder)
    {
        builder.ToTable("git_refs", "repo");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(r => r.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(r => r.CommitHash).HasColumnName("commit_hash").HasMaxLength(40).IsRequired();
        builder.Property(r => r.IsDefault).HasColumnName("is_default").IsRequired();

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<GitRepository>().WithMany().HasForeignKey(r => r.RepositoryId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.RepositoryId, r.Type, r.Name }).HasDatabaseName("ux_git_refs_repo_type_name").IsUnique();
        builder.HasIndex(r => new { r.RepositoryId, r.IsDefault })
               .HasDatabaseName("ux_git_refs_repo_default")
               .IsUnique()
               .HasFilter("is_default = true");
    }
}
