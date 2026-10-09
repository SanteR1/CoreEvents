using System.Text.Json;
using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Commands;
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
            case nameof(BookingReservationAccepted):
                await DispatchReservationAcceptedAsync(payload, ct);
                break;
            case nameof(BookingReservationRejected):
                await DispatchReservationRejectedAsync(payload, ct);
                break;
            case nameof(BookingReservationReleased):
                await DispatchReservationReleasedAsync(payload, ct);
                break;
            case nameof(EventCreated):
                await DispatchEventCreatedAsync(payload, ct);
                break;
            case nameof(EventUpdated):
                await DispatchEventUpdatedAsync(payload, ct);
                break;
            case nameof(EventCancelled):
                await DispatchEventCancelledAsync(payload, ct);
                break;
            case nameof(EventBookingValidationCompleted):
                await DispatchLegacyValidationAsync(payload, ct);
                break;
            case nameof(EventBookingCancellationCompleted):
                await DispatchLegacyCancellationAsync(payload, ct);
                break;
            default:
                throw new InvalidOperationException($"Неизвестный тип события: {eventType}");
        }
    }

    private async Task DispatchReservationAcceptedAsync(string payload, CancellationToken ct)
    {
        var e = Deserialize<BookingReservationAccepted>(payload);
        var command = new BookingReservationAcceptedCommand(
            e.BookingId,
            e.EventId,
            e.Seats,
            e.UnitPrice,
            e.TotalPrice,
            e.Currency,
            e.DiscountAmount,
            e.PriceVersion,
            e.ExpiresAt);
        await mediator.Send(command, ct);
    }

    private async Task DispatchReservationRejectedAsync(string payload, CancellationToken ct)
    {
        var e = Deserialize<BookingReservationRejected>(payload);
        await mediator.Send(new RejectBookingCommand(e.BookingId, e.RejectionReason), ct);
    }

    private async Task DispatchReservationReleasedAsync(string payload, CancellationToken ct)
    {
        var e = Deserialize<BookingReservationReleased>(payload);
        await mediator.Send(new ApplyBookingCancellationCommand(e.BookingId), ct);
    }

    private async Task DispatchEventCreatedAsync(string payload, CancellationToken ct)
    {
        var e = Deserialize<EventCreated>(payload);
        var command = new SyncEventCreatedCommand(
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
            e.CreatedAt);
        await mediator.Send(command, ct);
    }

    private async Task DispatchEventUpdatedAsync(string payload, CancellationToken ct)
    {
        var e = Deserialize<EventUpdated>(payload);
        var command = new SyncEventUpdatedCommand(
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
            e.UpdatedAt);
        await mediator.Send(command, ct);
    }

    private async Task DispatchEventCancelledAsync(string payload, CancellationToken ct)
    {
        var e = Deserialize<EventCancelled>(payload);
        var command = new SyncEventCancelledCommand(e.EventId, e.Reason, e.Version, e.CancelledAt);
        await mediator.Send(command, ct);
    }

    private async Task DispatchLegacyValidationAsync(string payload, CancellationToken ct)
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
    }

    private async Task DispatchLegacyCancellationAsync(string payload, CancellationToken ct)
    {
        var e = Deserialize<EventBookingCancellationCompleted>(payload);
        await mediator.Send(new ApplyBookingCancellationCommand(e.BookingId), ct);
    }

    private static T Deserialize<T>(string payload) =>
        JsonSerializer.Deserialize<T>(payload, IntegrationEventJsonOptions.Default)
        ?? throw new JsonException($"Invalid {typeof(T).Name} payload");
}
