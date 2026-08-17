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
        entity.RowVersion
    );

    public static List<EventCacheDto> FromEntity(List<Event> entity) => new(
        entity.Select(FromEntity).ToList()
    );
}
