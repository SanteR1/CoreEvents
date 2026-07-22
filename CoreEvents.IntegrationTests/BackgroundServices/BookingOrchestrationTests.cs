using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Enums;
using CoreEvents.IntegrationTests.Infrastructure.Bases;
using CoreEvents.IntegrationTests.Infrastructure.Factories;
using CoreEvents.Presentation.BackgroundServices;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEvents.IntegrationTests.BackgroundServices
{
    public class BookingOrchestrationTests(ApiOnlyIntegrationTestFactory factory) : ApiOnlyIntegrationTestBase(factory)
    {
        [Fact]
        public async Task Concurrency_ThreeServers_ShouldProcess90Bookings_WithoutDataCorruption()
        {
            // Arrange
            var user = User.Create("Test", "123", "Admin");
            var eventId = await ExecuteDbContextAsync(async db =>
            {
                db.Users.Add(user);
                var testEvent = Event.Create("Load Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 100);
                db.Events.Add(testEvent);

                for (int i = 0; i < 90; i++)
                {
                    var booking = Booking.Create(testEvent.Id, user.Id);
                    db.Bookings.Add(booking);
                }

                testEvent.ReleaseSeats(90);
                await db.SaveChangesAsync();
                return testEvent.Id;
            });
            
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var worker1 = ActivatorUtilities.CreateInstance<BookingProcessingService>(Factory.Services);
            var worker2 = ActivatorUtilities.CreateInstance<BookingProcessingService>(Factory.Services);
            var worker3 = ActivatorUtilities.CreateInstance<BookingProcessingService>(Factory.Services);

            // Act
            await Task.WhenAll(
                worker1.StartAsync(cts.Token),
                worker2.StartAsync(cts.Token),
                worker3.StartAsync(cts.Token)
            );
            
            var timeout = TimeSpan.FromSeconds(15);
            var isProcessed = await WaitUntilAsync(
                condition: async () =>
                {
                    return await ExecuteDbContextAsync(async db =>
                    {
                        var currentBookings = await db.Bookings
                            .Where(b => b.EventId == eventId)
                            .ToListAsync(cancellationToken: cts.Token);

                        return currentBookings.Count == 90 &&
                               currentBookings.All(b => b.Status == BookingStatus.Confirmed)
                               && currentBookings.All(b => b.ProcessedAt != null);
                    });
                },
                timeout: timeout,
                pollingInterval: TimeSpan.FromMilliseconds(100),
                testCancellationToken: TestContext.Current.CancellationToken);

            await Task.WhenAll(
                worker1.StopAsync(CancellationToken.None),
                worker2.StopAsync(CancellationToken.None),
                worker3.StopAsync(CancellationToken.None)
            );

            // Assert
            isProcessed.Should().BeTrue("Три параллельных сервера должны успеть обработать 90 броней");

            await ExecuteDbContextAsync(async db =>
            {
                var processedBookings = await db.Bookings.Where(b => b.EventId == eventId).ToListAsync(cancellationToken: cts.Token);
                processedBookings.Should().HaveCount(90);
                processedBookings.Should().OnlyContain(b => b.Status == BookingStatus.Confirmed,
                    "Потому что все 90 упавших транзакций должны были перейти в статус Rejected");
                processedBookings.Should().OnlyContain(b => b.ProcessedAt != null);
            });
        }
    }
}