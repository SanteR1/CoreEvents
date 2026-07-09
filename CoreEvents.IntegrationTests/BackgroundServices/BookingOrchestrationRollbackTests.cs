using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Enums;
using CoreEvents.IntegrationTests.Infrastructure.Bases;
using CoreEvents.IntegrationTests.Infrastructure.Factories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CoreEvents.IntegrationTests.BackgroundServices
{
    public class BookingOrchestrationRollbackTests(FaultInjectionTestFactory factory) : FaultInjectionTestBase(factory)
    {
        [Fact]
        public async Task ProcessBookingAsync_When90ConcurrentRollbacksViaRealFailure_ShouldReleaseSeatsCorrectlyAndRejected()
        {
            // Arrange
            var bookingIds = new List<Guid>();

            var eventId = await ExecuteDbContextAsync(async db =>
            {
                var testEvent = Event.Create("Load Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 100);
                db.Events.Add(testEvent);

                List<Booking> bookingList = new List<Booking>(90);

                for (int i = 0; i < 90; i++)
                {
                    var booking = Booking.Create(testEvent.Id);
                    bookingList.Add(booking);
                    bookingIds.Add(booking.Id);
                }
                db.Bookings.AddRange(bookingList);
                await db.SaveChangesAsync();
                return testEvent.Id;
            });

            State.TargetEventIdForFailures = eventId;

            // Act & Assert
            var timeout = TimeSpan.FromSeconds(15);
            var isProcessed = await WaitUntilAsync(
                condition: async () =>
                {
                    return await ExecuteDbContextAsync(async db =>
                    {
                        var currentBookings = await db.Bookings
                            .Where(b => bookingIds.Contains(b.Id))
                            .ToListAsync();

                        return currentBookings.Count == 90 &&
                               currentBookings.All(b => b.Status == BookingStatus.Rejected)
                               && currentBookings.All(b => b.ProcessedAt != null);
                    });
                },
                timeout: timeout,
                pollingInterval: TimeSpan.FromMilliseconds(100),
                testCancellationToken: TestContext.Current.CancellationToken);

            isProcessed.Should().BeTrue("Фоновая служба должна была подтвердить бронь в течение {0} секунд", timeout.TotalSeconds);

            await ExecuteDbContextAsync(async db =>
            {
                var processedBookings = await db.Bookings.Where(b => bookingIds.Contains(b.Id)).ToListAsync();
                processedBookings.Should().HaveCount(90);
                processedBookings.Should().OnlyContain(b => b.Status == BookingStatus.Rejected,
                    "Потому что все 90 упавших транзакций должны были перейти в статус Rejected");
                var processedEvent = await db.Events.FindAsync(eventId);
                processedEvent!.AvailableSeats.Should()
                    .Be(100, "Все 90 мест должны корректно вернуться без Lost Update");
            });
        }
    }
}