using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Repo;

public sealed class FileVersionConfiguration : IEntityTypeConfiguration<FileVersion>
{
    public void Configure(EntityTypeBuilder<FileVersion> builder)
    {
        builder.ToTable("file_versions", "repo");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(v => v.FileId).HasColumnName("file_id").HasColumnType("uuid").IsRequired();
        builder.Property(v => v.CommitHash).HasColumnName("commit_hash").HasMaxLength(40).IsRequired();
        builder.Property(v => v.BlobHash).HasColumnName("blob_hash").HasMaxLength(40).IsRequired();
        builder.Property(v => v.Size).HasColumnName("size").HasColumnType("bigint").IsRequired();
        builder.Property(v => v.Action).HasColumnName("action").HasConversion<string>().HasMaxLength(30).IsRequired();
        
        builder.Property(v => v.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<RepositoryFile>().WithMany().HasForeignKey(v => v.FileId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => new { v.FileId, v.CommitHash }).HasDatabaseName("ux_file_versions_file_commit").IsUnique();
    }
}
