using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Repo;

public sealed class RepositoryCredentialConfiguration : IEntityTypeConfiguration<RepositoryCredential>
{
    public void Configure(EntityTypeBuilder<RepositoryCredential> builder)
    {
        builder.ToTable("repository_credentials", "repo");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(c => c.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid").IsRequired();
        builder.Property(c => c.CredentialType).HasColumnName("credential_type").HasMaxLength(40).IsRequired();
        builder.Property(c => c.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(c => c.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz");
        
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Repository>().WithMany().HasForeignKey(c => c.RepositoryId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.RepositoryId, c.IsActive }).HasDatabaseName("ix_repository_credentials_repo_active");
    }
}
