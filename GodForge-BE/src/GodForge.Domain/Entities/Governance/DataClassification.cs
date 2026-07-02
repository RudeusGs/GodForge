using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Governance;

public sealed class DataClassification : BaseEntity
{
    public string SchemaName { get; private set; } = default!;
    public string TableName { get; private set; } = default!;
    public string? ColumnName { get; private set; }
    public string Classification { get; private set; } = default!;
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private DataClassification() { } // EF Core

    public static DataClassification Create(
        string schemaName, string tableName, string? columnName,
        string classification, string? notes, DateTimeOffset now)
    {
        return new DataClassification
        {
            Id = Guid.NewGuid(),
            SchemaName = schemaName,
            TableName = tableName,
            ColumnName = columnName,
            Classification = classification,
            Notes = notes,
            CreatedAt = now
        };
    }
}
