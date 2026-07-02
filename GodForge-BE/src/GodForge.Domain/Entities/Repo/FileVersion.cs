using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Repo;

public sealed class FileVersion : BaseEntity
{
    public Guid FileId { get; private set; }
    public string CommitHash { get; private set; } = default!;
    public string BlobHash { get; private set; } = default!;
    public long Size { get; private set; }
    public string Action { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private FileVersion() { } // EF Core

    public static FileVersion Create(Guid fileId, string commitHash, string blobHash, long size, string action, DateTimeOffset now)
    {
        return new FileVersion
        {
            Id = Guid.NewGuid(),
            FileId = fileId,
            CommitHash = commitHash,
            BlobHash = blobHash,
            Size = size,
            Action = action,
            CreatedAt = now
        };
    }
}
