using CoreEvents.Shared.Contracts.Events;
using Events.Domain.Entities;
using Events.Domain.Enums;

namespace Events.Application.DTOs;

public record SeatReservationResponseDto(
    Guid BookingId,
    Guid EventId,
    int Seats,
    decimal UnitPrice,
    decimal TotalPrice,
    string Currency,
    decimal DiscountAmount,
    long PriceVersion,
    SeatReservationStatus Status,
    ValidationFailureReason? RejectionReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt
)
{
    public static SeatReservationResponseDto FromEntity(SeatReservation entity) => new(
        entity.BookingId,
        entity.EventId,
        entity.Seats,
        entity.UnitPrice,
        entity.TotalPrice,
        entity.Currency,
        entity.DiscountAmount,
        entity.PriceVersion,
        entity.Status,
        entity.RejectionReason,
        entity.CreatedAt,
        entity.ExpiresAt
    );
}
