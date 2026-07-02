using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Repo;

public sealed class RepositoryCredentialVersionConfiguration : IEntityTypeConfiguration<RepositoryCredentialVersion>
{
    public void Configure(EntityTypeBuilder<RepositoryCredentialVersion> builder)
    {
        builder.ToTable("repository_credential_versions", "repo");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(v => v.CredentialId).HasColumnName("credential_id").HasColumnType("uuid").IsRequired();
        builder.Property(v => v.EncryptedSecret).HasColumnName("encrypted_secret").HasColumnType("text").IsRequired();
        builder.Property(v => v.EncryptionKeyId).HasColumnName("encryption_key_id").HasMaxLength(120).IsRequired();
        builder.Property(v => v.CreatedBy).HasColumnName("created_by").HasColumnType("uuid").IsRequired();

        builder.Property(v => v.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<RepositoryCredential>().WithMany().HasForeignKey(v => v.CredentialId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(v => v.CreatedBy).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.CredentialId, v.CreatedAt }).HasDatabaseName("ix_repository_credential_versions_time").IsDescending(false, true);
    }
}
