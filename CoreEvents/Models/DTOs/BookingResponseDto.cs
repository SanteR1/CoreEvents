using System.Linq.Expressions;
using CoreEvents.Models.Domain;

namespace CoreEvents.Models.DTOs
{
    public record BookingResponseDto(
        Guid Id,
        Guid EventId,
        BookingStatus Status,
        DateTime CreatedAt,
        DateTime? ProcessedAt
        )
    {
        public static Expression<Func<Booking, BookingResponseDto>> ToDto => booking => new BookingResponseDto(
            booking.Id,
            booking.Guid,
            booking.Status,
            booking.CreatedAt,
            booking.ProcessedAt
        );

        public static Func<Booking, BookingResponseDto> ToDtoCompile => ToDto.Compile();
    }
}
