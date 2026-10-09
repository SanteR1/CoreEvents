using System.Text.Json;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Serialization;
using Events.Application.Abstractions.Messaging;
using Events.Application.Commands;
using MediatR;

namespace Events.Infrastructure.Messaging;

internal sealed class BookingRequestDispatcher(IMediator mediator) : IIntegrationEventDispatcher
{
    public async Task DispatchAsync(string eventType, string payload, CancellationToken ct)
    {
        switch (eventType)
        {
            case nameof(BookingReservationRequested):
                {
                    var e = Deserialize<BookingReservationRequested>(payload);
                    await mediator.Send(new BookingReservationRequestCommand(e.BookingId, e.EventId, e.UserId, e.Seats), ct);
                    break;
                }
            case nameof(BookingReservationReleaseRequested):
                {
                    var e = Deserialize<BookingReservationReleaseRequested>(payload);
                    await mediator.Send(new BookingReservationReleaseRequestCommand(e.BookingId, e.EventId, e.Seats, e.Reason), ct);
                    break;
                }
            case nameof(BookingConfirmed):
                {
                    var e = Deserialize<BookingConfirmed>(payload);
                    await mediator.Send(new ValidateBookingCommand(e.BookingId, e.EventId, e.Seats), ct);

                    break;
                }
            case nameof(BookingCreated):
                {
                    var e = Deserialize<BookingCreated>(payload);
                    await mediator.Send(new ValidateBookingCommand(e.BookingId, e.EventId, e.Seats), ct);

                    break;
                }
            case nameof(BookingCancellationRequested):
                {
                    var e = Deserialize<BookingCancellationRequested>(payload);
                    await mediator.Send(new ReleaseSeatsCommand(e.BookingId, e.EventId, e.Seats), ct);
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
