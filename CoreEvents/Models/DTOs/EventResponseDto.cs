using System.Linq.Expressions;
using CoreEvents.Models.Domain;

namespace CoreEvents.Models.DTOs
{
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
        public static Expression<Func<EventEntity, EventResponseDto>> ToDto => entity => new EventResponseDto(
            entity.Id,
            entity.Title,
            entity.Description,
            entity.StartAt,
            entity.EndAt,
            entity.TotalSeats,
            entity.AvailableSeats
        );
        public static Func<EventEntity, EventResponseDto> ToDtoCompiled => ToDto.Compile();
    }
}
