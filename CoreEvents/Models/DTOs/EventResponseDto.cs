using System.Linq.Expressions;
using CoreEvents.Models.Domain;

namespace CoreEvents.Models.DTOs
{
    public record EventResponseDto(
        Guid Id,
        string Title,
        string? Description,
        DateTime StartAt,
        DateTime EndAt
    )
    {
        public static Expression<Func<EventEntity, EventResponseDto>> ToDto => entity => new EventResponseDto(
            entity.Id,
            entity.Title,
            entity.Description,
            entity.StartAt,
            entity.EndAt
        );
        public static Func<EventEntity, EventResponseDto> ToDtoCompiled => ToDto.Compile();
    }
}
