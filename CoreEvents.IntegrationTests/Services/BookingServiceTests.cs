using CoreEvents.Application.DTOs;
using CoreEvents.Application.Services;
using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Enums;
using CoreEvents.Domain.Exceptions;
using CoreEvents.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
        public async Task CreateBookingAsync_MultipleBookingsForSameEvent_ShouldAssignStatusIsPending()
        {
            // Arrange 
            const int initialSeats = 10;
            var eventId = await ExecuteDbContextAsync(async db =>
            {
                var event1 = Event.Create("TEST Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), initialSeats);
                db.Events.Add(event1);
                await db.SaveChangesAsync();
                return event1.Id;
            });
            var requestDto = new BookingCreateDto(eventId);

            // Act
            var results = await Task.WhenAll(
                Enumerable.Range(0, initialSeats)
                    .Select(_ => Task.Run(() => ExecuteScopeAsync(async sp =>
                        {
                            var bookingService = sp.GetRequiredService<IBookingService>();
                            return await bookingService.CreateBookingAsync(requestDto);
                        }))));

            // Assert
            results.Should().HaveCount(initialSeats);
            results.Should().AllSatisfy(b =>
            {
                b.Should().NotBeNull();
                b.Status.Should().Be(BookingStatus.Pending);
            });

            await ExecuteDbContextAsync(async db =>
            {
                var savedBookings = await db.Bookings
                    .Where(b => b.EventId == eventId)
                    .ToListAsync();

                savedBookings.Should().HaveCount(initialSeats);
                savedBookings.Should().AllSatisfy(b =>
                {
                    b.Status.Should().Be(BookingStatus.Pending);
                    b.ProcessedAt.Should().BeNull();
                });

                var updatedEvent = await db.Events.FindAsync(eventId);
                updatedEvent!.AvailableSeats.Should().Be(0);
            });
        }
        
        [Fact]
        public async Task CreateBookingAsync_MultipleBookingsForSameEvent_ShouldAssignUniqueIds()
        {
            // Arrange 
            const int initialSeats = 10;
            var existEvent = await ExecuteDbContextAsync(async db =>
            {
                var evet1 = Event.Create("TEST Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), initialSeats);
                db.Events.Add(evet1);
                await db.SaveChangesAsync();
                return evet1;
            });
            var requestDto = new BookingCreateDto(existEvent.Id);

            // Act
            var results = await Task.WhenAll(
                Enumerable.Range(0, initialSeats)
                    .Select(_ => Task.Run(() => ExecuteScopeAsync(async sp =>
                    {
                        var bookingService = sp.GetRequiredService<IBookingService>();
                        return await bookingService.CreateBookingAsync(requestDto);
                    }))));

            // Assert
            results.Should().HaveCount(initialSeats);

            await ExecuteDbContextAsync(async db =>
            {
                var savedBookings = await db.Bookings
                    .Where(b => b.EventId == existEvent.Id)
                    .ToListAsync();

                savedBookings.Select(r => r.Id)
                    .Should().OnlyHaveUniqueItems()
                    .And.HaveCount(initialSeats);
                savedBookings.Should().OnlyContain(b => b.EventId == existEvent.Id);

                var updatedEvent = await db.Events.FindAsync(existEvent.Id);
                updatedEvent!.AvailableSeats.Should().Be(0);
            });
        }

        [Fact]
        public async Task CreateBookingAsync_WhenMultipleConcurrentRequests_ShouldAssignUniqueIds()
        {
            // Arrange
            const int initialSeats = 10;
            const int totalRequests = 10;
            const int expectedSuccesses = 10;
            var existEvent = await ExecuteDbContextAsync(async db =>
            {
                var evet1 = Event.Create("TEST Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), initialSeats);
                db.Events.Add(evet1);
                await db.SaveChangesAsync();
                return evet1;
            });
            var requestDto = new BookingCreateDto(existEvent.Id);

            // Act
            var results = await Task.WhenAll(
                Enumerable.Range(0, totalRequests)
                    .Select(_ => Task.Run(() => ExecuteScopeAsync(async sp =>
                    {
                        var bookingService = sp.GetRequiredService<IBookingService>();
                        return await bookingService.CreateBookingAsync(requestDto);
                    }))));

            // Assert
            results.Should().HaveCount(expectedSuccesses);

            await ExecuteDbContextAsync(async db =>
            {
                var savedBookings = await db.Bookings
                    .Where(b => b.EventId == existEvent.Id)
                    .ToListAsync();

                savedBookings.Select(r => r.Id)
                    .Should().OnlyHaveUniqueItems()
                    .And.HaveCount(expectedSuccesses);
                savedBookings.Should().OnlyContain(b => b.EventId == existEvent.Id);

                var updatedEvent = await db.Events.FindAsync(existEvent.Id);
                updatedEvent!.AvailableSeats.Should().Be(0);
            });
        }

        [Fact]
        public async Task CreateBookingAsync_WhenMultipleConcurrentRequests_ShouldPreventOverbooking()
        {
            // Arrange
            const int initialSeats = 5;
            const int totalRequests = 20;
            const int expectedSuccesses = 5;
            var existEvent = await ExecuteDbContextAsync(async db =>
            {
                var evet1 = Event.Create("TEST Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), initialSeats);
                db.Events.Add(evet1);
                await db.SaveChangesAsync();
                return evet1;
            });
            var requestDto = new BookingCreateDto(existEvent.Id);

            // Act
            var tasks = Enumerable.Range(0, totalRequests)
                    .Select(_ => Task.Run(() => ExecuteScopeAsync(async sp =>
                    {
                        var bookingService = sp.GetRequiredService<IBookingService>();
                        return await bookingService.CreateBookingAsync(requestDto);
                    })))
                    .ToList();

            var allTasks = Task.WhenAll(tasks);
            try
            {
                await allTasks;
            }
            catch { }

            // Assert
            var exceptions = (allTasks.Exception?.InnerExceptions ?? Enumerable.Empty<Exception>()).ToList();
            tasks.Where(t => t.Status == TaskStatus.RanToCompletion)
                .Should().HaveCount(expectedSuccesses);

            exceptions.OfType<DomainNoAvailableSeatsException>()
                .Should().HaveCount(totalRequests - expectedSuccesses);

            exceptions.Where(e => e is not DomainNoAvailableSeatsException)
                .Should().BeEmpty();

            await ExecuteDbContextAsync(async db =>
            {
                var savedBookings = await db.Bookings
                    .Where(b => b.EventId == existEvent.Id)
                    .ToListAsync();

                savedBookings.Select(r => r.Id)
                    .Should().OnlyHaveUniqueItems()
                    .And.HaveCount(expectedSuccesses);
                savedBookings.Should().OnlyContain(b => b.EventId == existEvent.Id);

                var updatedEvent = await db.Events.FindAsync(existEvent.Id);
                updatedEvent!.AvailableSeats.Should().Be(0);
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
