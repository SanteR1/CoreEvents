namespace CoreEvents.Application.DTOs
{
    public record PaginatedResult<T>
    {
        public required int TotalCount { get; init; }
        public required IReadOnlyList<T> Items { get; init; }
        public required int CurrentPage { get; init; }
        public required int PageSize { get; init; }
        public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    }
}
