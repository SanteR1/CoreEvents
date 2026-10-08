using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Abstractions.Resilience.Attributes;
using Bookings.Application.Abstractions.Resilience.Constants;
using Bookings.Domain.Enums;
using CoreEvents.Shared.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bookings.Application.Commands;

[ResiliencePipeline(ResiliencePipelines.CommandConcurrency)]
public record BookingReservationAcceptedCommand(
    Guid BookingId,
    Guid EventId,
    int Seats,
    decimal UnitPrice,
    decimal TotalPrice,
    string Currency,
    decimal DiscountAmount,
    long PriceVersion,
    DateTimeOffset? ExpiresAt) : ICommand<Unit>;

internal class BookingReservationAcceptedHandler(
    IBookingRepository repository,
    IOutboxService outboxService,
    ILogger<BookingReservationAcceptedHandler> logger) : IRequestHandler<BookingReservationAcceptedCommand, Unit>
{
    public async Task<Unit> Handle(BookingReservationAcceptedCommand request, CancellationToken cancellationToken)
    {
        var booking = await repository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking == null)
        {
            logger.LogWarning("Booking with ID {BookingId} not found for reservation accepted. Message ignored.", request.BookingId);
            return Unit.Value;
        }

        if (booking.Status == BookingStatus.Pending)
        {
            booking.Confirm(request.UnitPrice, request.TotalPrice, request.Currency, request.DiscountAmount);
            repository.Update(booking);

            outboxService.Publish(
                new BookingConfirmed
                {
                    BookingId = booking.Id,
                    EventId = booking.EventId,
                    UserId = booking.UserId,
                    Seats = booking.Seats
                },
                partitionKey: booking.EventId.ToString());

            await repository.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }

        // Late Arrival Edge Case: booking is already Rejected or Cancelled
        if (booking.Status is BookingStatus.Rejected or BookingStatus.Cancelled or BookingStatus.CancellationPending)
        {
            logger.LogWarning(
                "Late reservation accepted for Booking {BookingId} which is already in status {Status}. Compensating reservation release.",
                booking.Id, booking.Status);

            outboxService.Publish(
                new BookingReservationReleaseRequested
                {
                    BookingId = booking.Id,
                    EventId = booking.EventId,
                    Seats = request.Seats > 0 ? request.Seats : booking.Seats,
                    Reason = CancellationReason.Timeout
                },
                partitionKey: booking.EventId.ToString());

            await repository.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }

        // Idempotent: already Confirmed
        return Unit.Value;
    }
}
