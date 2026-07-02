using GodForge.Domain.Entities.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Metadata;

public sealed class ScriptSymbolConfiguration : IEntityTypeConfiguration<ScriptSymbol>
{
    public void Configure(EntityTypeBuilder<ScriptSymbol> builder)
    {
        builder.ToTable("script_symbols", "metadata");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(s => s.ScriptId).HasColumnName("script_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.SymbolType).HasColumnName("symbol_type").HasMaxLength(40).IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(s => s.Signature).HasColumnName("signature").HasColumnType("text");
        builder.Property(s => s.LineNumber).HasColumnName("line_number");
        builder.Property(s => s.MetadataJson).HasColumnName("metadata").HasColumnType("jsonb");

        builder.HasOne<Script>().WithMany().HasForeignKey(s => s.ScriptId).OnDelete(DeleteBehavior.Cascade);
    }
}
