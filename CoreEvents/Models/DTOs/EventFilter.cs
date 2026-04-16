namespace CoreEvents.Models.DTOs
{
    public record EventFilter(
        string? Title = null,
        DateTime? From = null,
        DateTime? To = null,
        int Page = 1,
        int PageSize = 10
    ):PagedFilter(Page,PageSize);
}
