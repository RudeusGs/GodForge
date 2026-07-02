using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Entities.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Search;

public sealed class SearchDocumentConfiguration : IEntityTypeConfiguration<SearchDocument>
{
    public void Configure(EntityTypeBuilder<SearchDocument> builder)
    {
        builder.ToTable("search_documents", "search");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(d => d.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(d => d.RepositoryId).HasColumnName("repository_id").HasColumnType("uuid");
        builder.Property(d => d.SnapshotId).HasColumnName("snapshot_id").HasColumnType("uuid");
        builder.Property(d => d.EntityType).HasColumnName("entity_type").HasMaxLength(50).IsRequired();
        builder.Property(d => d.EntityId).HasColumnName("entity_id").HasColumnType("uuid").IsRequired();
        builder.Property(d => d.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(d => d.Subtitle).HasColumnName("subtitle").HasMaxLength(500);
        builder.Property(d => d.Content).HasColumnName("content").HasColumnType("text");
        builder.Property(d => d.Path).HasColumnName("path").HasMaxLength(800);
#pragma warning disable CS0618 // Type or member is obsolete: Client-side parsing of NpgsqlTsVector is unreliable. Used here solely to bridge Clean Architecture string property to DB tsvector without leaking Npgsql to Domain.
        builder.Property(d => d.SearchVector)
            .HasColumnName("search_vector")
            .HasColumnType("tsvector")
            .HasConversion(
                v => string.IsNullOrEmpty(v) ? null : NpgsqlTypes.NpgsqlTsVector.Parse(v),
                v => v == null ? null : v.ToString()
            );
#pragma warning restore CS0618
        builder.Property(d => d.MetadataJson).HasColumnName("metadata").HasColumnType("jsonb");

        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Project>().WithMany().HasForeignKey(d => d.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<GitRepository>().WithMany().HasForeignKey(d => d.RepositoryId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<RepositorySnapshot>().WithMany().HasForeignKey(d => d.SnapshotId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(d => new { d.ProjectId, d.EntityType }).HasDatabaseName("ix_search_documents_project_type");
        // GIN index requires raw SQL mapping in migration, but we can set the method here in EF Core 9
        builder.HasIndex(d => d.SearchVector).HasDatabaseName("ix_search_documents_vector").HasMethod("gin");
    }
}
