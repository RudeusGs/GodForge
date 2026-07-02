using GodForge.Domain.Entities.Governance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Governance;

public sealed class DataClassificationConfiguration : IEntityTypeConfiguration<DataClassification>
{
    public void Configure(EntityTypeBuilder<DataClassification> builder)
    {
        builder.ToTable("data_classifications", "governance");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(c => c.SchemaName).HasColumnName("schema_name").HasMaxLength(80).IsRequired();
        builder.Property(c => c.TableName).HasColumnName("table_name").HasMaxLength(120).IsRequired();
        builder.Property(c => c.ColumnName).HasColumnName("column_name").HasMaxLength(120);
        builder.Property(c => c.Classification).HasColumnName("classification").HasMaxLength(40).IsRequired();
        builder.Property(c => c.Notes).HasColumnName("notes").HasColumnType("text");

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
    }
}
