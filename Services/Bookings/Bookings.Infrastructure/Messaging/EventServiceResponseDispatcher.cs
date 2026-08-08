using System.Text.Json;
using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Commands;
using Bookings.Application.Commands.Bookings.Application.Commands;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Serialization;
using MediatR;

namespace Bookings.Infrastructure.Messaging;

internal sealed class EventServiceResponseDispatcher(IMediator mediator) : IIntegrationEventDispatcher
{
    public async Task DispatchAsync(string eventType, string payload, CancellationToken ct)
    {
        switch (eventType)
        {
            case nameof(EventBookingValidationCompleted):
                {
                    var e = Deserialize<EventBookingValidationCompleted>(payload);
                    if (e.CanBeBooked)
                    {
                        await mediator.Send(new ConfirmBookingCommand(e.EventId, e.BookingId), ct);
                    }
                    else
                    {
                        var reason = e.FailureReason.GetValueOrDefault();
                        await mediator.Send(new RejectBookingCommand(e.BookingId, reason), ct);
                    }
                    break;
                }
            case nameof(EventBookingCancellationCompleted):
                {
                    var e = Deserialize<EventBookingCancellationCompleted>(payload);
                    await mediator.Send(new ApplyBookingCancellationCommand(e.BookingId), ct);
                    break;
                }
            default:
                throw new InvalidOperationException($"Неизвестный тип события: {eventType}");
        }
    }

    private static T Deserialize<T>(string payload) =>
        JsonSerializer.Deserialize<T>(payload, IntegrationEventJsonOptions.Default)
        ?? throw new JsonException($"Invalid {typeof(T).Name} payload");
}
