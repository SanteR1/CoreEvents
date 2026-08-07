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
    int AvailableSeats
)
{
    public static Expression<Func<Event, EventResponseDto>> ToDto => entity => new EventResponseDto(
        entity.Id,
        entity.Title,
        entity.Description,
        entity.StartAt,
        entity.EndAt,
        entity.TotalSeats,
        entity.AvailableSeats
    );

    public static EventResponseDto FromEntity(Event entity) => new(
        entity.Id,
        entity.Title,
        entity.Description,
        entity.StartAt,
        entity.EndAt,
        entity.TotalSeats,
        entity.AvailableSeats
    );
}
