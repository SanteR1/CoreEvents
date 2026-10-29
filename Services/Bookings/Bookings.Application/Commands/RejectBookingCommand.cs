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
public record RejectBookingCommand(Guid BookingId, ValidationFailureReason Reason) : ICommand<Unit>;

internal class RejectBookingHandler(
    IBookingRepository repository,
    IOutboxService outboxService,
    ILogger<RejectBookingHandler> logger) : IRequestHandler<RejectBookingCommand, Unit>
{
    public async Task<Unit> Handle(RejectBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await repository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking == null)
        {
            logger.LogWarning("Booking with ID {BookingId} not found for Reject. Message ignored.", request.BookingId);
            return Unit.Value;
        }

        if (booking.Status == BookingStatus.Pending)
        {
            booking.Reject(request.Reason);
            repository.Update(booking);

            outboxService.Publish(
                new BookingRejected
                {
                    BookingId = booking.Id,
                    EventId = booking.EventId,
                    UserId = booking.UserId,
                    Reason = request.Reason,
                    RejectedAt = DateTimeOffset.UtcNow
                },
                partitionKey: booking.EventId.ToString());

            await repository.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
