using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Metadata;

public sealed class ScriptSymbol : BaseEntity
{
    public Guid ScriptId { get; private set; }
    public string SymbolType { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Signature { get; private set; }
    public int? LineNumber { get; private set; }
    public string? MetadataJson { get; private set; }

    private ScriptSymbol() { } // EF Core

    public static ScriptSymbol Create(
        Guid scriptId, string symbolType, string name,
        string? signature, int? lineNumber, string? metadataJson)
    {
        return new ScriptSymbol
        {
            Id = Guid.NewGuid(),
            ScriptId = scriptId,
            SymbolType = symbolType,
            Name = name,
            Signature = signature,
            LineNumber = lineNumber,
            MetadataJson = metadataJson
        };
    }
}
