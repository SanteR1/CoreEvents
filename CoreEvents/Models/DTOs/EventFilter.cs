using System.ComponentModel.DataAnnotations;

namespace CoreEvents.Models.DTOs
{
    public class EventFilter : PagedFilter
    {
        public string? Title { get; init; }
        public DateTime? From { get; init; }
        public DateTime? To { get; init; }
    }
}
