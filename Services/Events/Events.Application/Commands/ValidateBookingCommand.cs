using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Repositories;
using Events.Application.Abstractions.Resilience.Attributes;
using Events.Application.Abstractions.Resilience.Constants;
using Events.Domain.Enums;
using MediatR;

namespace Events.Application.Commands;

[ResiliencePipeline(ResiliencePipelines.CommandConcurrency)]
public record ValidateBookingCommand(
    Guid BookingId,
    Guid EventId,
    int Seats,
    InboxEnvelope Inbox) : IInboxCommand
{
    public ValidateBookingCommand(Guid bookingId, Guid eventId, int seats)
        : this(bookingId, eventId, seats, InboxEnvelope.CreateDefault("booking.created.v1")) { }
}

internal class ValidateBookingHandler(IEventRepository repository, IOutboxService outboxService) : IRequestHandler<ValidateBookingCommand, InboxProcessingResult>
{
    public async Task<InboxProcessingResult> Handle(ValidateBookingCommand request, CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
        {
            outboxService.AddIntegrationEvent(
                new EventBookingValidationCompleted
                {
                    BookingId = request.BookingId,
                    EventId = request.EventId,
                    FailureReason = ValidationFailureReason.EventNotFound,
                    CanBeBooked = false
                },
                partitionKey: request.EventId.ToString());

            return InboxProcessingResult.Processed;
        }

        if (@event.StartAt <= DateTime.UtcNow)
        {
            outboxService.AddIntegrationEvent(
                new EventBookingValidationCompleted
                {
                    BookingId = request.BookingId,
                    EventId = request.EventId,
                    FailureReason = ValidationFailureReason.EventAlreadyPassed,
                    CanBeBooked = false
                },
                partitionKey: request.EventId.ToString());

            return InboxProcessingResult.Processed;
        }

        if (@event.TryReserveSeats(request.BookingId, request.Seats, DateTimeOffset.UtcNow) != ReserveSeatsResult.Reserved)
        {
            outboxService.AddIntegrationEvent(
                new EventBookingValidationCompleted
                {
                    BookingId = request.BookingId,
                    EventId = request.EventId,
                    FailureReason = ValidationFailureReason.SeatsNotAvailable,
                    CanBeBooked = false
                },
                partitionKey: request.EventId.ToString());

            return InboxProcessingResult.Processed;
        }

        outboxService.AddIntegrationEvent(
            new EventBookingValidationCompleted
            {
                BookingId = request.BookingId,
                EventId = request.EventId,
                CanBeBooked = true
            },
            partitionKey: request.EventId.ToString());

        return InboxProcessingResult.Processed;
    }
}
