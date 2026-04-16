namespace CoreEvents.Models.DTOs
{
    public record EventFilter(
        string? Title = null,
        DateTime? From = null,
        DateTime? To = null
    );
}
