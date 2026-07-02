using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Search;

public sealed class SavedSearch : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Query { get; private set; } = default!;
    public string? FiltersJson { get; private set; }

    private SavedSearch() { } // EF Core

    public static SavedSearch Create(
        Guid userId, string name, string query, string? filtersJson)
    {
        return new SavedSearch
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Query = query,
            FiltersJson = filtersJson
        };
    }
}
