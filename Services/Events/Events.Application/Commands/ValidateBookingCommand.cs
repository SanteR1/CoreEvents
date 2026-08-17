using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Repositories;
using Events.Application.Abstractions.Resilience.Attributes;
using Events.Application.Abstractions.Resilience.Constants;
using MediatR;

namespace Events.Application.Commands;

[ResiliencePipeline(ResiliencePipelines.CommandConcurrency)]
public record ValidateBookingCommand(Guid BookingId, Guid EventId, int Seats) : ICommand<Unit>;

internal class ValidateBookingHandler(IEventRepository repository, IOutboxService outboxService) : IRequestHandler<ValidateBookingCommand, Unit>
{
    public async Task<Unit> Handle(ValidateBookingCommand request, CancellationToken ct)
    {
        var @event = await repository.GetByIdAsync(request.EventId, ct);
        if (@event == null)
        {
            outboxService.Publish(
                new EventBookingValidationCompleted
                {
                    BookingId = request.BookingId,
                    EventId = request.EventId,
                    FailureReason = ValidationFailureReason.EventNotFound,
                    CanBeBooked = false
                },
                partitionKey: request.EventId.ToString());

            return Unit.Value;
        }

        if (@event.StartAt <= DateTime.UtcNow)
        {
            outboxService.Publish(
                new EventBookingValidationCompleted
                {
                    BookingId = request.BookingId,
                    EventId = request.EventId,
                    FailureReason = ValidationFailureReason.EventAlreadyPassed,
                    CanBeBooked = false
                },
                partitionKey: request.EventId.ToString());

            return Unit.Value;
        }

        if (!@event.TryReserveSeats(request.Seats))
        {
            outboxService.Publish(
                new EventBookingValidationCompleted
                {
                    BookingId = request.BookingId,
                    EventId = request.EventId,
                    FailureReason = ValidationFailureReason.SeatsNotAvailable,
                    CanBeBooked = false
                },
                partitionKey: request.EventId.ToString());

            return Unit.Value;
        }

        outboxService.Publish(
            new EventBookingValidationCompleted()
            {
                BookingId = request.BookingId,
                EventId = request.EventId,
                CanBeBooked = true
            },
            partitionKey: request.EventId.ToString());

        return Unit.Value;
    }
}
