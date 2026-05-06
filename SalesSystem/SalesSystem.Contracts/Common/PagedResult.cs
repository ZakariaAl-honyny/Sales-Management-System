namespace SalesSystem.Contracts.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; private set; } = Array.Empty<T>();
    public int TotalCount { get; private set; }
    public int Page { get; private set; }
    public int PageSize { get; private set; }
    public int TotalPages { get; private set; }
    public bool HasNext { get; private set; }
    public bool HasPrevious { get; private set; }

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNext = page < totalPages,
            HasPrevious = page > 1
        };
    }
}