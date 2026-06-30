using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Enums;

namespace CoreEvents.Application.DTOs
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
        public static Expression<Func<Booking, BookingResponseDto>> ToDto => booking => new BookingResponseDto(
            booking.Id,
            booking.EventId,
            booking.Status,
            booking.CreatedAt,
            booking.ProcessedAt
        );

        public static BookingResponseDto FromEntity(Booking booking) => new(
            booking.Id,
            booking.EventId,
            booking.Status,
            booking.CreatedAt,
            booking.ProcessedAt
        );
    }
}
