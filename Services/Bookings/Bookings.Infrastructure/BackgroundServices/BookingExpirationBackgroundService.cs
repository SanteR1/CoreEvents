using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Repositories;
using CoreEvents.Shared.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bookings.Infrastructure.BackgroundServices;

public sealed class BookingExpirationBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingExpirationBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan PendingTimeout = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan CancellationPendingStaleThreshold = TimeSpan.FromMinutes(10);
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BookingExpirationBackgroundService started.");

        using var timer = new PeriodicTimer(CheckInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessExpiredPendingBookingsAsync(stoppingToken);
                await ProcessStaleCancellationPendingBookingsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during booking expiration check.");
            }
        }

        logger.LogInformation("BookingExpirationBackgroundService stopped.");
    }

    private async Task ProcessExpiredPendingBookingsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        var threshold = DateTime.UtcNow.Subtract(PendingTimeout);
        var expiredBookings = await bookingRepository.GetExpiredPendingBookingsAsync(threshold, BatchSize, ct);

        foreach (var booking in expiredBookings)
        {
            logger.LogWarning("Booking {BookingId} has timed out in Pending status. Rejecting and sending release request.", booking.Id);

            booking.Reject(ValidationFailureReason.Timeout);
            bookingRepository.Update(booking);

            outboxService.Publish(
                new BookingReservationReleaseRequested
                {
                    BookingId = booking.Id,
                    EventId = booking.EventId,
                    Seats = booking.Seats,
                    Reason = CancellationReason.Timeout
                },
                partitionKey: booking.EventId.ToString());

            outboxService.Publish(
                new BookingRejected
                {
                    BookingId = booking.Id,
                    EventId = booking.EventId,
                    UserId = booking.UserId,
                    Reason = ValidationFailureReason.Timeout,
                    RejectedAt = DateTimeOffset.UtcNow
                },
                partitionKey: booking.EventId.ToString());
        }

        if (expiredBookings.Count > 0)
        {
            await bookingRepository.SaveChangesAsync(ct);
        }
    }

    private async Task ProcessStaleCancellationPendingBookingsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        var threshold = DateTimeOffset.UtcNow.Subtract(CancellationPendingStaleThreshold);
        var staleBookings = await bookingRepository.GetStaleCancellationPendingBookingsAsync(threshold, BatchSize, ct);

        foreach (var booking in staleBookings)
        {
            logger.LogWarning("Booking {BookingId} is stale in CancellationPending status. Retrying release request.", booking.Id);

            outboxService.Publish(
                new BookingReservationReleaseRequested
                {
                    BookingId = booking.Id,
                    EventId = booking.EventId,
                    Seats = booking.Seats,
                    Reason = booking.CancellationReason ?? CancellationReason.UserCancelled
                },
                partitionKey: booking.EventId.ToString());
        }

        if (staleBookings.Count > 0)
        {
            await bookingRepository.SaveChangesAsync(ct);
        }
    }
}
