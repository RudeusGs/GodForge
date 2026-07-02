namespace GodForge.Application.Common.Models;

public interface IPagedResult
{
    object ItemsObject { get; }
    int Page { get; }
    int PageSize { get; }
    int TotalItems { get; }
    int TotalPages { get; }
}

public class PagedResult<T> : IPagedResult
{
    public IReadOnlyList<T> Items { get; }
    public object ItemsObject => Items;
    public int Page { get; }
    public int PageSize { get; }
    public int TotalItems { get; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

    public PagedResult(IEnumerable<T> items, int page, int pageSize, int totalItems)
    {
        Items = items.ToList().AsReadOnly();
        Page = page;
        PageSize = pageSize;
        TotalItems = totalItems;
    }
}
