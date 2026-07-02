using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Search;

public sealed class SearchDocument : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid? RepositoryId { get; private set; }
    public Guid? SnapshotId { get; private set; }
    public string EntityType { get; private set; } = default!;
    public Guid EntityId { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Subtitle { get; private set; }
    public string? Content { get; private set; }
    public string? Path { get; private set; }
    public string? SearchVector { get; private set; } // Represented as string/tsvector mapped in EF Core
    public string? MetadataJson { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private SearchDocument() { } // EF Core

    public static SearchDocument Create(
        Guid projectId, Guid? repositoryId, Guid? snapshotId,
        string entityType, Guid entityId, string title,
        string? subtitle, string? content, string? path,
        string? searchVector, string? metadataJson, DateTimeOffset now)
    {
        return new SearchDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RepositoryId = repositoryId,
            SnapshotId = snapshotId,
            EntityType = entityType,
            EntityId = entityId,
            Title = title,
            Subtitle = subtitle,
            Content = content,
            Path = path,
            SearchVector = searchVector,
            MetadataJson = metadataJson,
            UpdatedAt = now
        };
    }
}
