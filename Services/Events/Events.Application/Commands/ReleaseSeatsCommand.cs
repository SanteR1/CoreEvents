using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Repositories;
using Events.Application.Abstractions.Resilience.Attributes;
using Events.Application.Abstractions.Resilience.Constants;
using MediatR;

namespace Events.Application.Commands
{
    [ResiliencePipeline(ResiliencePipelines.CommandConcurrency)]
    public record ReleaseSeatsCommand(Guid BookingId, Guid EventId, int Seats) : ICommand<Unit>;

    internal class ReleaseSeatsHandler(IEventRepository repository, IOutboxService outboxService) : IRequestHandler<ReleaseSeatsCommand, Unit>
    {
        public async Task<Unit> Handle(ReleaseSeatsCommand request, CancellationToken ct)
        {
            var @event = await repository.GetByIdAsync(request.EventId, ct);
            if (@event == null)
            {
                outboxService.Publish(
                    new EventBookingCancellationCompleted
                    {
                        BookingId = request.BookingId,
                        EventId = request.EventId,
                        SeatsReleased = false
                    },
                    partitionKey: request.EventId.ToString());

                return Unit.Value;
            }

            if (!@event.ReleaseSeats(request.Seats))
            {
                outboxService.Publish(
                    new EventBookingCancellationCompleted()
                    {
                        BookingId = request.BookingId,
                        EventId = request.EventId,
                        SeatsReleased = false
                    },
                    partitionKey: request.EventId.ToString());

                return Unit.Value;
            }

            outboxService.Publish(
                new EventBookingCancellationCompleted()
                {
                    BookingId = request.BookingId,
                    EventId = request.EventId,
                    SeatsReleased = true
                },
                partitionKey: request.EventId.ToString());

            return Unit.Value;
        }
    }
}
