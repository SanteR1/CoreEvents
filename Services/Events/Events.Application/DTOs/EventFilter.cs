namespace Events.Application.DTOs;

public record EventFilter : PagedFilter
{
    public string? Title { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? Currency { get; init; }
    public bool? IsActive { get; init; }
}
