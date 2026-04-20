namespace CoreEvents.Models.DTOs
{
    public record PaginatedResult(
        int TotalEvents,
        IEnumerable<EventResponseDto> Events,
        int CurrentPage,
        int PageSize);
}
