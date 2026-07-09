using CoreEvents.Application.DTOs;
using CoreEvents.Application.Services;
using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Enums;
using CoreEvents.IntegrationTests.Infrastructure.Bases;
using CoreEvents.IntegrationTests.Infrastructure.Factories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEvents.IntegrationTests.BackgroundServices;

public class BookingProcessingHostTests(IntegrationTestFactory factory) : SharedIntegrationTestBase(factory)
{
    [Fact]
    public async Task E2E_ProcessBooking_ShouldBeProcessedAndConfirmedByBackgroundService()
    {
        // Arrange
        var eventId = await ExecuteDbContextAsync(async db =>
        {
            var testEvent = Event.Create("E2E Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
            db.Events.Add(testEvent);
            await db.SaveChangesAsync();
            return testEvent.Id;
        });

        var bookingId = await ExecuteScopeAsync(async sp =>
        {
            var bookingService = sp.GetRequiredService<IBookingService>();
            var result = await bookingService.CreateBookingAsync(new BookingCreateDto(eventId), TestContext.Current.CancellationToken);
            return result.Id;
        });

        await ExecuteDbContextAsync(async db =>
        {
            var booking = await db.Bookings.FindAsync(bookingId);
            booking!.Status.Should().Be(BookingStatus.Pending, "На этом этапе бронь ещё не должна быть обработана воркером");
        });

        // Act
        var timeout = TimeSpan.FromSeconds(15);
        var isProcessed = await WaitUntilAsync(
            condition: async () =>
            {
                return await ExecuteDbContextAsync(async db =>
                {
                    var booking = await db.Bookings.FindAsync(bookingId);
                    return booking != null && booking.Status == BookingStatus.Confirmed;
                });
            },
            timeout: timeout,
            pollingInterval: TimeSpan.FromMilliseconds(100),
            testCancellationToken: TestContext.Current.CancellationToken);


        // Assert
        isProcessed.Should().BeTrue("Фоновая служба должна была подтвердить бронь в течение {0} секунд", timeout.TotalSeconds);

        await ExecuteDbContextAsync(async db =>
        {
            var processedBooking = await db.Bookings.FindAsync(bookingId);
            var processedEvent = await db.Events.FindAsync(eventId);
            processedBooking.Should().NotBeNull();
            processedBooking!.Status.Should().Be(BookingStatus.Confirmed);
            processedBooking!.ProcessedAt.Should().NotBeNull();
            processedEvent!.AvailableSeats.Should().Be(9);
        });
    }
}