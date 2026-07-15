using GodForge.Domain.Entities.Analysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Analysis;

public sealed class AiFindingConfiguration : IEntityTypeConfiguration<AiFinding>
{
    public void Configure(EntityTypeBuilder<AiFinding> builder)
    {
        builder.ToTable("ai_findings", "analysis");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(x => x.RunId).HasColumnName("run_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Severity).HasColumnName("severity").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text").IsRequired();
        builder.Property(x => x.Recommendation).HasColumnName("recommendation").HasColumnType("text");
        builder.Property(x => x.Confidence).HasColumnName("confidence").HasPrecision(5, 4);
        builder.Property(x => x.EvidenceRefsJson).HasColumnName("evidence_refs").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<AiAnalysisRun>()
            .WithMany()
            .HasForeignKey(x => x.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.RunId, x.Fingerprint })
            .HasDatabaseName("ux_ai_findings_run_fingerprint")
            .IsUnique();
    }
}
