using System.Diagnostics.CodeAnalysis;

namespace Bookings.Application.DTOs;

public record PaginatedResult<T>
{
    public required int TotalCount { get; init; }
    public required IReadOnlyList<T> Items { get; init; }
    public required int CurrentPage { get; init; }
    public required int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (TotalCount + PageSize - 1) / PageSize : 0;

    public PaginatedResult() { }

    [SetsRequiredMembers]
    public PaginatedResult(IReadOnlyList<T> items, int totalCount, int currentPage, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        CurrentPage = currentPage;
        PageSize = pageSize;
    }

    public PaginatedResult<TDestination> Map<TDestination>(Func<T, TDestination> mapFunc)
    {
        return new PaginatedResult<TDestination>
        {
            CurrentPage = CurrentPage,
            PageSize = PageSize,
            TotalCount = TotalCount,
            Items = Items.Select(mapFunc).ToList()
        };
    }
}
