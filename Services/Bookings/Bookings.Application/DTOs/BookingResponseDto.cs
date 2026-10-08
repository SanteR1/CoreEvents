using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Bookings.Domain.Entities;
using Bookings.Domain.Enums;
using CoreEvents.Shared.Contracts.Events;

namespace Bookings.Application.DTOs;

public record BookingResponseDto(
    [Required]
    Guid Id,
    Guid EventId,
    Guid UserId,
    int Seats,
    BookingStatus Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    decimal TotalPrice,
    decimal UnitPrice,
    decimal DiscountAmount,
    string Currency,
    string? RejectionReason,
    CancellationReason? CancellationReason,
    DateTimeOffset? CancellationRequestedAt
    )
{
    public static Expression<Func<Booking, BookingResponseDto>> ToDto => booking => new BookingResponseDto(
        booking.Id,
        booking.EventId,
        booking.UserId,
        booking.Seats,
        booking.Status,
        booking.CreatedAt,
        booking.ProcessedAt,
        booking.TotalPrice,
        booking.UnitPrice,
        booking.DiscountAmount,
        booking.Currency,
        booking.RejectionReason,
        booking.CancellationReason,
        booking.CancellationRequestedAt
    );

    public static BookingResponseDto FromEntity(Booking booking) => new(
        booking.Id,
        booking.EventId,
        booking.UserId,
        booking.Seats,
        booking.Status,
        booking.CreatedAt,
        booking.ProcessedAt,
        booking.TotalPrice,
        booking.UnitPrice,
        booking.DiscountAmount,
        booking.Currency,
        booking.RejectionReason,
        booking.CancellationReason,
        booking.CancellationRequestedAt
    );
}
