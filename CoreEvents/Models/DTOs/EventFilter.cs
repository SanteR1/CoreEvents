namespace CoreEvents.Models.DTOs
{
    public record EventFilter : PagedFilter
    {
        public string? Title { get; init; }
        public DateTime? From { get; init; }
        public DateTime? To { get; init; }
    }
}
