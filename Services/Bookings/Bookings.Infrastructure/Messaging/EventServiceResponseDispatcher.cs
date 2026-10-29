using System.Text.Json;
using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Commands;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Exceptions;
using CoreEvents.Shared.Contracts.Registry;
using CoreEvents.Shared.Contracts.Serialization;
using MediatR;

namespace Bookings.Infrastructure.Messaging;

internal sealed class EventServiceResponseDispatcher : IIntegrationEventDispatcher
{
    private readonly Dictionary<string, Func<string, CancellationToken, Task>> _handlers
        = new(StringComparer.Ordinal);
    private readonly IIntegrationEventRegistry _registry;

    public EventServiceResponseDispatcher(IMediator mediator, IIntegrationEventRegistry? registry = null)
    {
        _registry = registry ?? new IntegrationEventRegistry();
        RegisterBookingResponses(mediator);
        RegisterEventLifecycleResponses(mediator);
    }

    private void RegisterBookingResponses(IMediator mediator)
    {
        Register<BookingReservationAccepted>((e, ct) =>
            mediator.Send(new BookingReservationAcceptedCommand(
                e.BookingId,
                e.EventId,
                e.Seats,
                e.UnitPrice,
                e.TotalPrice,
                e.Currency,
                e.DiscountAmount,
                e.PriceVersion,
                e.ExpiresAt), ct));

        Register<BookingReservationRejected>((e, ct) =>
            mediator.Send(new RejectBookingCommand(e.BookingId, e.RejectionReason), ct));

        Register<BookingReservationReleased>((e, ct) =>
            mediator.Send(new ApplyBookingCancellationCommand(e.BookingId), ct));

        Register<EventBookingValidationCompleted>((e, ct) =>
        {
            if (e.CanBeBooked)
            {
                return mediator.Send(new ConfirmBookingCommand(e.EventId, e.BookingId), ct);
            }
            var reason = e.FailureReason.GetValueOrDefault();
            return mediator.Send(new RejectBookingCommand(e.BookingId, reason), ct);
        });

        Register<EventBookingCancellationCompleted>((e, ct) =>
            mediator.Send(new ApplyBookingCancellationCommand(e.BookingId), ct));
    }

    private void RegisterEventLifecycleResponses(IMediator mediator)
    {
        Register<EventCreated>((e, ct) =>
            mediator.Send(new SyncEventCreatedCommand(
                e.EventId,
                e.Title,
                e.StartAt,
                e.EndAt,
                e.UnitPrice,
                e.Currency,
                e.PriceVersion,
                e.TotalSeats,
                e.IsActive,
                e.Version,
                e.CreatedAt), ct));

        Register<EventUpdated>((e, ct) =>
            mediator.Send(new SyncEventUpdatedCommand(
                e.EventId,
                e.Title,
                e.StartAt,
                e.EndAt,
                e.UnitPrice,
                e.Currency,
                e.PriceVersion,
                e.TotalSeats,
                e.IsActive,
                e.Version,
                e.UpdatedAt), ct));

        Register<EventCancelled>((e, ct) =>
            mediator.Send(new SyncEventCancelledCommand(e.EventId, e.Reason, e.Version, e.CancelledAt), ct));
    }

    public IReadOnlyCollection<string> SupportedEventTypes => _handlers.Keys;

    public Task DispatchAsync(string eventType, string payload, CancellationToken ct) =>
        _handlers.TryGetValue(eventType, out var h)
            ? h(payload, ct)
            : throw new UnknownEventTypeException(eventType);

    private void Register<T>(Func<T, CancellationToken, Task> handle) where T : IIntegrationEvent =>
        _handlers[_registry.GetName<T>()] = (p, ct) => handle(Deserialize<T>(p), ct);

    private static T Deserialize<T>(string payload) =>
        JsonSerializer.Deserialize<T>(payload, IntegrationEventJsonOptions.Default)
        ?? throw new JsonException($"Invalid {typeof(T).Name} payload");
}
