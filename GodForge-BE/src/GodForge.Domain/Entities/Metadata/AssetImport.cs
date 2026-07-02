using System.Collections.Generic;
using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Metadata;

public sealed class AssetImport : BaseEntity
{
    public Guid AssetId { get; private set; }
    public string? Importer { get; private set; }
    public string? ImportType { get; private set; }
    public string? Uid { get; private set; }
    public string? SourceFile { get; private set; }
    public List<string> DestFiles { get; private set; } = new();
    public string? ImportOptionsJson { get; private set; }

    private AssetImport() { } // EF Core

    public static AssetImport Create(
        Guid assetId, string? importer, string? importType,
        string? uid, string? sourceFile, List<string> destFiles,
        string? importOptionsJson)
    {
        return new AssetImport
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Importer = importer,
            ImportType = importType,
            Uid = uid,
            SourceFile = sourceFile,
            DestFiles = destFiles ?? new List<string>(),
            ImportOptionsJson = importOptionsJson
        };
    }
}
