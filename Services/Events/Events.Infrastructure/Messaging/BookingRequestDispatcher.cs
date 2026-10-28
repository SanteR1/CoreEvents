using System.Text.Json;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Exceptions;
using CoreEvents.Shared.Contracts.Registry;
using CoreEvents.Shared.Contracts.Serialization;
using Events.Application.Abstractions.Messaging;
using Events.Application.Commands;
using MediatR;

namespace Events.Infrastructure.Messaging;

internal sealed class BookingRequestDispatcher : IIntegrationEventDispatcher
{
    private readonly Dictionary<string, Func<string, InboxEnvelope, CancellationToken, Task>> _handlers
        = new(StringComparer.Ordinal);
    private readonly IIntegrationEventRegistry _registry;

    public BookingRequestDispatcher(IMediator mediator, IIntegrationEventRegistry registry)
    {
        _registry = registry;

        Register<BookingReservationRequested>((e, env, ct) =>
            mediator.Send(new BookingReservationRequestCommand(e.BookingId, e.EventId, e.UserId, e.Seats, env), ct));
        Register<BookingReservationReleaseRequested>((e, env, ct) =>
            mediator.Send(new BookingReservationReleaseRequestCommand(e.BookingId, e.EventId, e.Seats, e.Reason, env), ct));
        Register<BookingCancelled>((e, env, ct) =>
            mediator.Send(new SyncBookingCancelledCommand(e.BookingId, e.EventId, env), ct));
        Register<BookingRejected>((e, env, ct) =>
            mediator.Send(new SyncBookingCancelledCommand(e.BookingId, e.EventId, env), ct));
        Register<BookingCreated>((e, env, ct) =>
            mediator.Send(new ValidateBookingCommand(e.BookingId, e.EventId, e.Seats, env), ct));
        Register<BookingCancellationRequested>((e, env, ct) =>
            mediator.Send(new ReleaseSeatsCommand(e.BookingId, e.EventId, e.Seats, env), ct));

        // Осознанный no-op: места резервируются на BookingReservationRequested
        RegisterNoOp<BookingConfirmed>((env, ct) =>
            mediator.Send(new ProcessNoOpInboxCommand(env), ct));
    }

    public IReadOnlyCollection<string> SupportedEventTypes => _handlers.Keys;

    public Task DispatchAsync(string eventType, string payload, InboxEnvelope env, CancellationToken ct) =>
        _handlers.TryGetValue(eventType, out var h)
            ? h(payload, env, ct)
            : throw new UnknownEventTypeException(eventType);

    private void Register<T>(Func<T, InboxEnvelope, CancellationToken, Task> handle) where T : IIntegrationEvent =>
        _handlers[_registry.GetName<T>()] = (p, env, ct) => handle(Deserialize<T>(p), env, ct);

    private void RegisterNoOp<T>(Func<InboxEnvelope, CancellationToken, Task> handle) where T : IIntegrationEvent =>
        _handlers[_registry.GetName<T>()] = (_, env, ct) => handle(env, ct);

    private static T Deserialize<T>(string payload) =>
        JsonSerializer.Deserialize<T>(payload, IntegrationEventJsonOptions.Default)
        ?? throw new JsonException($"Invalid {typeof(T).Name} payload");
}
