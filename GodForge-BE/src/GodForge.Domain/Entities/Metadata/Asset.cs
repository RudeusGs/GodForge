using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Metadata;

public sealed class Asset : BaseEntity
{
    public Guid RepositoryId { get; private set; }
    public Guid SnapshotId { get; private set; }
    public Guid MetadataRunId { get; private set; }
    public string FilePath { get; private set; } = default!;
    public string FileName { get; private set; } = default!;
    public string AssetType { get; private set; } = default!;
    public long FileSizeBytes { get; private set; }
    public string? MimeType { get; private set; }
    public string? DimensionsJson { get; private set; }
    public Guid? ThumbnailArtifactId { get; private set; }
    public string FileHash { get; private set; } = default!;
    public string? ImportedMetadataJson { get; private set; }

    private Asset() { } // EF Core

    public static Asset Create(
        Guid repositoryId, Guid snapshotId, Guid metadataRunId,
        string filePath, string fileName, string assetType,
        long fileSizeBytes, string? mimeType, string? dimensionsJson,
        Guid? thumbnailArtifactId, string fileHash, string? importedMetadataJson)
    {
        return new Asset
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            SnapshotId = snapshotId,
            MetadataRunId = metadataRunId,
            FilePath = filePath,
            FileName = fileName,
            AssetType = assetType,
            FileSizeBytes = fileSizeBytes,
            MimeType = mimeType,
            DimensionsJson = dimensionsJson,
            ThumbnailArtifactId = thumbnailArtifactId,
            FileHash = fileHash,
            ImportedMetadataJson = importedMetadataJson
        };
    }
}
