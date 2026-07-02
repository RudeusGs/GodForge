using GodForge.Domain.Entities.Storage;
using GodForge.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Storage;

public sealed class PreviewRequestConfiguration : IEntityTypeConfiguration<PreviewRequest>
{
    public void Configure(EntityTypeBuilder<PreviewRequest> builder)
    {
        builder.ToTable("preview_requests", "storage");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(r => r.ProjectId).HasColumnName("project_id").HasColumnType("uuid").IsRequired();
        builder.Property(r => r.AssetId).HasColumnName("asset_id").HasMaxLength(100).IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(r => r.OutputPath).HasColumnName("output_path").HasMaxLength(500);
        builder.Property(r => r.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
        
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Project>().WithMany().HasForeignKey(r => r.ProjectId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.ProjectId, r.AssetId }).HasDatabaseName("ix_preview_requests_project_asset");
    }
}
