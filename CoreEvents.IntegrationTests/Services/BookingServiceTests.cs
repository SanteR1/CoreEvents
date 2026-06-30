using CoreEvents.Application.DTOs;
using CoreEvents.Application.Services;
using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Enums;
using CoreEvents.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEvents.IntegrationTests.Services
{
    public class BookingServiceTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
    {
        [Fact]
        public async Task CreateBookingAsync_WithValidData_ShouldReturnSuccessResultAndSaveToDb()
        {
            // Arrange 
            var eventId = await ExecuteDbContextAsync(async db =>
            {
                var event1 = Event.Create("TEST Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
                db.Events.Add(event1);
                await db.SaveChangesAsync();
                return event1.Id;
            });
            var requestDto = new BookingCreateDto(eventId);

            // Act
            var result = await ExecuteScopeAsync(sp =>
            {
                var bookingService = sp.GetRequiredService<IBookingService>();
                return bookingService.CreateBookingAsync(requestDto);
            });

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBe(Guid.Empty);

            await ExecuteDbContextAsync(async db =>
            {
                var savedBooking = await db.Bookings.FindAsync(result.Id);

                savedBooking.Should().NotBeNull();
                savedBooking.EventId.Should().Be(eventId);
                savedBooking.Status.Should().Be(BookingStatus.Pending);
                savedBooking.ProcessedAt.Should().BeNull();
            });
        }

        [Fact]
        public async Task GetBookingByIdAsync_WithExistId_ShouldReturnSuccessBooking()
        {
            // Arrange 
            var eventId = await ExecuteDbContextAsync(async db =>
            {
                var event1 = Event.Create("TEST Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
                db.Events.Add(event1);
                await db.SaveChangesAsync();
                return event1.Id;
            });
            var requestDto = new BookingCreateDto(eventId);
            var existBooking = await ExecuteScopeAsync(sp =>
            {
                var bookingService = sp.GetRequiredService<IBookingService>();
                return bookingService.CreateBookingAsync(requestDto);
            });

            // Act
            var result = await ExecuteScopeAsync(sp =>
            {
                var bookingService = sp.GetRequiredService<IBookingService>();
                return bookingService.GetBookingByIdAsync(existBooking.Id);
            });

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(existBooking.Id);
            result.EventId.Should().Be(existBooking.EventId);
            result.Status.Should().Be(existBooking.Status);
        }
    }
}
