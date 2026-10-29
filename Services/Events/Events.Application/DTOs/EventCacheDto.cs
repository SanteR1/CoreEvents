using Events.Domain.Entities;

namespace Events.Application.DTOs;

public record EventCacheDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt,
    int TotalSeats,
    int AvailableSeats,
    decimal Price,
    string Currency,
    bool IsActive,
    long Version,
    long PriceVersion,
    uint RowVersion
)
{
    public static EventCacheDto FromEntity(Event entity) => new(
        entity.Id,
        entity.Title,
        entity.Description,
        entity.StartAt,
        entity.EndAt,
        entity.TotalSeats,
        entity.AvailableSeats,
        entity.Price,
        entity.Currency,
        entity.IsActive,
        entity.Version,
        entity.PriceVersion,
        entity.RowVersion
    );

    public static IReadOnlyList<EventCacheDto> FromEntity(IEnumerable<Event> entity) =>
        entity.Select(FromEntity).ToList();
}
