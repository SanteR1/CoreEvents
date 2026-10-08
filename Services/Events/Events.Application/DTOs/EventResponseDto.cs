using System.Linq.Expressions;
using Events.Domain.Entities;

namespace Events.Application.DTOs;

public record EventResponseDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt,
    int TotalSeats,
    int AvailableSeats,
    decimal Price,
    string Currency,
    bool IsActive
)
{
    public static Expression<Func<Event, EventResponseDto>> ToDto => entity => new EventResponseDto(
        entity.Id,
        entity.Title,
        entity.Description,
        entity.StartAt,
        entity.EndAt,
        entity.TotalSeats,
        entity.AvailableSeats,
        entity.Price,
        entity.Currency,
        entity.IsActive
    );

    public static EventResponseDto FromEntity(Event entity) => new(
        entity.Id,
        entity.Title,
        entity.Description,
        entity.StartAt,
        entity.EndAt,
        entity.TotalSeats,
        entity.AvailableSeats,
        entity.Price,
        entity.Currency,
        entity.IsActive
    );

    public static EventResponseDto FromEntity(EventCacheDto entity) => new(
        entity.Id,
        entity.Title,
        entity.Description,
        entity.StartAt,
        entity.EndAt,
        entity.TotalSeats,
        entity.AvailableSeats,
        entity.Price,
        entity.Currency,
        entity.IsActive
    );

    public static IReadOnlyList<EventResponseDto> FromEntity(IEnumerable<Event> entity) =>
        entity.Select(FromEntity).ToList();

    public static IReadOnlyList<EventResponseDto> FromEntity(IEnumerable<EventCacheDto> entity) =>
        entity.Select(FromEntity).ToList();
}
