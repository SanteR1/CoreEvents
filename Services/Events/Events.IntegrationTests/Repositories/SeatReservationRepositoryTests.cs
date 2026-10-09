using AwesomeAssertions;
using Events.Application.Abstractions.Repositories;
using Events.Domain.Entities;
using Events.Domain.Enums;
using Events.IntegrationTests.Infrastructure.Bases;
using Events.IntegrationTests.Infrastructure.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Events.IntegrationTests.Repositories;

public class SeatReservationRepositoryTests(ApiOnlyIntegrationTestFactory factory) : ApiOnlyIntegrationTestBase(factory)
{
    [Fact]
    public async Task Add_AndSave_ShouldPersistSeatReservation()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var reservation = SeatReservation.CreateReserved(
            bookingId: bookingId,
            eventId: eventId,
            seats: 2,
            unitPrice: 1000m,
            currency: "KZT",
            discountAmount: 0m,
            priceVersion: 1L,
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(15),
            correlationId: Guid.NewGuid(),
            causationId: Guid.NewGuid());

        // Act
        await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<ISeatReservationRepository>();
            repo.Add(reservation);
            await repo.SaveChangesAsync();
        });

        // Assert
        await ExecuteDbContextAsync(async ctx =>
        {
            var inDb = await ctx.SeatReservations.FirstOrDefaultAsync(r => r.BookingId == bookingId);
            inDb.Should().NotBeNull();
            inDb.BookingId.Should().Be(bookingId);
            inDb.EventId.Should().Be(eventId);
            inDb.Seats.Should().Be(2);
            inDb.UnitPrice.Should().Be(1000m);
            inDb.TotalPrice.Should().Be(2000m);
            inDb.Currency.Should().Be("KZT");
            inDb.Status.Should().Be(SeatReservationStatus.Reserved);
        });
    }

    [Fact]
    public async Task GetByBookingIdAsync_WhenExists_ShouldReturnReservation()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var reservation = SeatReservation.CreateReserved(
            bookingId: bookingId,
            eventId: eventId,
            seats: 1,
            unitPrice: 500m,
            currency: "KZT",
            discountAmount: 0m,
            priceVersion: 1L,
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(15),
            correlationId: Guid.NewGuid(),
            causationId: Guid.NewGuid());

        await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<ISeatReservationRepository>();
            repo.Add(reservation);
            await repo.SaveChangesAsync();
        });

        // Act
        var result = await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<ISeatReservationRepository>();
            return await repo.GetByBookingIdAsync(bookingId);
        });

        // Assert
        result.Should().NotBeNull();
        result.BookingId.Should().Be(bookingId);
        result.EventId.Should().Be(eventId);
        result.TotalPrice.Should().Be(500m);
    }

    [Fact]
    public async Task GetByBookingIdAsync_WhenNotExists_ShouldReturnNull()
    {
        // Act
        var result = await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<ISeatReservationRepository>();
            return await repo.GetByBookingIdAsync(Guid.NewGuid());
        });

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEventIdAsync_ShouldReturnAllReservationsForEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var res1 = SeatReservation.CreateReserved(
            Guid.NewGuid(), eventId, 1, 500m, "KZT", 0m, 1L, DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());
        var res2 = SeatReservation.CreateReserved(
            Guid.NewGuid(), eventId, 2, 500m, "KZT", 0m, 1L, DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());
        var resOther = SeatReservation.CreateReserved(
            Guid.NewGuid(), Guid.NewGuid(), 1, 500m, "KZT", 0m, 1L, DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());

        await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<ISeatReservationRepository>();
            repo.Add(res1);
            repo.Add(res2);
            repo.Add(resOther);
            await repo.SaveChangesAsync();
        });

        // Act
        var results = await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<ISeatReservationRepository>();
            return await repo.GetByEventIdAsync(eventId);
        });

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(2);
        results.Select(r => r.BookingId).Should().Contain([res1.BookingId, res2.BookingId]);
    }
}
