using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using CoreEvents.Models.Domain;

namespace CoreEvents.Models.DTOs
{
    public record BookingResponseDto(
        [Required]
        Guid Id,
        Guid EventId,
        BookingStatus Status,
        DateTime CreatedAt,
        DateTime? ProcessedAt
        )
    {
        internal static Expression<Func<Booking, BookingResponseDto>> ToDto => booking => new BookingResponseDto(
            booking.Id,
            booking.EventId,
            booking.Status,
            booking.CreatedAt,
            booking.ProcessedAt
        );

        internal static BookingResponseDto FromEntity(Booking booking) => new(
            booking.Id,
            booking.EventId,
            booking.Status,
            booking.CreatedAt,
            booking.ProcessedAt
        );
    }
}
