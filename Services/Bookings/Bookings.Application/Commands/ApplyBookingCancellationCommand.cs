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
public record ApplyBookingCancellationCommand(Guid BookingId) : ICommand<Unit>;

internal class ApplyBookingCancellationCommandHandler(
    IBookingRepository repository,
    IOutboxService outboxService,
    ILogger<ApplyBookingCancellationCommandHandler> logger) : IRequestHandler<ApplyBookingCancellationCommand, Unit>
{
    public async Task<Unit> Handle(ApplyBookingCancellationCommand request, CancellationToken cancellationToken)
    {
        var booking = await repository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking == null)
        {
            logger.LogWarning("Booking with ID {BookingId} not found for cancellation. Message ignored.", request.BookingId);
            return Unit.Value;
        }

        if (booking.Status is BookingStatus.CancellationPending or BookingStatus.Confirmed or BookingStatus.Pending)
        {
            booking.ApplyCancellation();
            repository.Update(booking);

            outboxService.Publish(
                new BookingCancelled
                {
                    BookingId = booking.Id,
                    EventId = booking.EventId,
                    UserId = booking.UserId,
                    Reason = booking.CancellationReason ?? CancellationReason.UserCancelled,
                    CancelledAt = DateTimeOffset.UtcNow
                },
                partitionKey: booking.EventId.ToString());

            await repository.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
