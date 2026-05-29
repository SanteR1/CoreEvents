namespace CoreEvents.Models.DTOs
{
    public record PaginatedResult
    {
        public required int TotalCount { get; init; }
        public required IEnumerable<EventResponseDto> Items { get; init; }
        public required int CurrentPage { get; init; }
        public required int PageSize { get; init; }
        public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    }
}
