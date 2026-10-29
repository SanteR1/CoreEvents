using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Abstractions.Resilience.Attributes;
using Bookings.Application.Abstractions.Resilience.Constants;
using Bookings.Domain.Entities;
using CoreEvents.Shared.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bookings.Application.Commands;

[ResiliencePipeline(ResiliencePipelines.CommandConcurrency)]
public record SyncEventCreatedCommand(
    Guid EventId,
    string Title,
    DateTime StartAt,
    DateTime EndAt,
    decimal UnitPrice,
    string Currency,
    long PriceVersion,
    int TotalSeats,
    bool IsActive,
    long Version,
    DateTimeOffset CreatedAt) : ICommand<Unit>;

[ResiliencePipeline(ResiliencePipelines.CommandConcurrency)]
public record SyncEventUpdatedCommand(
    Guid EventId,
    string Title,
    DateTime StartAt,
    DateTime EndAt,
    decimal UnitPrice,
    string Currency,
    long PriceVersion,
    int TotalSeats,
    bool IsActive,
    long Version,
    DateTimeOffset UpdatedAt) : ICommand<Unit>;

[ResiliencePipeline(ResiliencePipelines.CommandConcurrency)]
public record SyncEventCancelledCommand(
    Guid EventId,
    string Reason,
    long Version,
    DateTimeOffset CancelledAt) : ICommand<Unit>;

internal class EventLifecycleHandlers(
    IEventProjectionRepository eventProjectionRepository,
    IBookingRepository bookingRepository,
    IOutboxService outboxService,
    ILogger<EventLifecycleHandlers> logger)
    : IRequestHandler<SyncEventCreatedCommand, Unit>,
      IRequestHandler<SyncEventUpdatedCommand, Unit>,
      IRequestHandler<SyncEventCancelledCommand, Unit>
{
    public async Task<Unit> Handle(SyncEventCreatedCommand request, CancellationToken cancellationToken)
    {
        var existing = await eventProjectionRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (existing != null)
        {
            logger.LogInformation("Event projection for {EventId} already exists. Updating if version is higher.", request.EventId);
            existing.Update(
                request.Title,
                request.StartAt,
                request.EndAt,
                request.UnitPrice,
                request.Currency,
                request.PriceVersion,
                request.TotalSeats,
                request.IsActive,
                request.Version,
                request.CreatedAt);
            eventProjectionRepository.Update(existing);
        }
        else
        {
            var projection = EventProjection.Create(
                request.EventId,
                request.Title,
                request.StartAt,
                request.EndAt,
                request.UnitPrice,
                request.Currency,
                request.PriceVersion,
                request.TotalSeats,
                request.IsActive,
                request.Version,
                request.CreatedAt);
            await eventProjectionRepository.AddAsync(projection, cancellationToken);
        }

        await eventProjectionRepository.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async Task<Unit> Handle(SyncEventUpdatedCommand request, CancellationToken cancellationToken)
    {
        var existing = await eventProjectionRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (existing == null)
        {
            logger.LogWarning("Event projection for {EventId} not found on update. Creating projection.", request.EventId);
            var projection = EventProjection.Create(
                request.EventId,
                request.Title,
                request.StartAt,
                request.EndAt,
                request.UnitPrice,
                request.Currency,
                request.PriceVersion,
                request.TotalSeats,
                request.IsActive,
                request.Version,
                request.UpdatedAt);
            await eventProjectionRepository.AddAsync(projection, cancellationToken);
        }
        else
        {
            existing.Update(
                request.Title,
                request.StartAt,
                request.EndAt,
                request.UnitPrice,
                request.Currency,
                request.PriceVersion,
                request.TotalSeats,
                request.IsActive,
                request.Version,
                request.UpdatedAt);
            eventProjectionRepository.Update(existing);
        }

        await eventProjectionRepository.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async Task<Unit> Handle(SyncEventCancelledCommand request, CancellationToken cancellationToken)
    {
        var projection = await eventProjectionRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (projection != null)
        {
            projection.Cancel(request.Version, request.CancelledAt);
            eventProjectionRepository.Update(projection);
            await eventProjectionRepository.SaveChangesAsync(cancellationToken);
        }

        const int batchSize = 100;
        while (!cancellationToken.IsCancellationRequested)
        {
            var activeBookings = await bookingRepository.GetActiveBookingsByEventIdAsync(request.EventId, batchSize, cancellationToken);
            if (activeBookings.Count == 0)
            {
                break;
            }

            foreach (var booking in activeBookings)
            {
                booking.ApplyCancellation(CancellationReason.EventCancelled);
                bookingRepository.Update(booking);

                outboxService.Publish(
                    new BookingCancelled
                    {
                        BookingId = booking.Id,
                        EventId = booking.EventId,
                        UserId = booking.UserId,
                        Reason = CancellationReason.EventCancelled,
                        CancelledAt = DateTimeOffset.UtcNow
                    },
                    partitionKey: booking.EventId.ToString());
            }

            await bookingRepository.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
