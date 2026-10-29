using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Domain.DomainEvents;

namespace Events.Application.Events;

internal sealed class PublishBookingReservationAcceptedHandler(
    IOutboxService outboxService)
    : IPreCommitHandler<SeatsReserved>
{
    public Task Handle(SeatsReserved e, CancellationToken ct)
    {
        outboxService.AddIntegrationEvent(
            new BookingReservationAccepted
            {
                BookingId = e.BookingId,
                EventId = e.EventId,
                Seats = e.Seats,
                UnitPrice = e.UnitPrice,
                TotalPrice = e.Seats * e.UnitPrice,
                Currency = e.Currency,
                DiscountAmount = 0.00m,
                PriceVersion = 1,
                ExpiresAt = e.OccurredAt.AddMinutes(15)
            },
            partitionKey: e.EventId.ToString());

        return Task.CompletedTask;
    }
}
