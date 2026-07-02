using GodForge.Domain.Entities.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Metadata;

public sealed class AssetImportConfiguration : IEntityTypeConfiguration<AssetImport>
{
    public void Configure(EntityTypeBuilder<AssetImport> builder)
    {
        builder.ToTable("asset_imports", "metadata");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(i => i.AssetId).HasColumnName("asset_id").HasColumnType("uuid").IsRequired();
        builder.Property(i => i.Importer).HasColumnName("importer").HasMaxLength(120);
        builder.Property(i => i.ImportType).HasColumnName("import_type").HasMaxLength(120);
        builder.Property(i => i.Uid).HasColumnName("uid").HasMaxLength(120);
        builder.Property(i => i.SourceFile).HasColumnName("source_file").HasMaxLength(800);
        builder.Property(i => i.DestFiles).HasColumnName("dest_files").HasColumnType("text[]");
        builder.Property(i => i.ImportOptionsJson).HasColumnName("import_options").HasColumnType("jsonb");

        builder.HasOne<Asset>().WithMany().HasForeignKey(i => i.AssetId).OnDelete(DeleteBehavior.Cascade);
    }
}
