using Bookings.Application.Abstractions.Repositories;
using Bookings.Domain.Entities;
using Bookings.Domain.Enums;
using Bookings.IntegrationTests.Infrastructure.Bases;
using Bookings.IntegrationTests.Infrastructure.Factories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bookings.IntegrationTests.Repositories;

public class BookingRepositoryTests(ApiOnlyIntegrationTestFactory factory) : ApiOnlyIntegrationTestBase(factory)
{
    [Fact]
    public async Task Add_ExistEventId_ShouldInsertBookingWithPendingStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var booking = Booking.Create(eventId, userId);

        // Act
        await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IBookingRepository>();
            repo.Add(booking);
            await repo.SaveChangesAsync();
        });

        // Assert
        await ExecuteDbContextAsync(async ctx =>
        {
            var exists = await ctx.Bookings.FindAsync(booking.Id);

            exists.Should().NotBeNull();
            booking.Id.Should().Be(exists.Id);
            eventId.Should().Be(exists.EventId);
        });
    }

    [Fact]
    public async Task Delete_ShouldRemoveBooking()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingId = await ExecuteDbContextAsync(async ctx =>
        {
            var b = Booking.Create(userId, userId);
            ctx.Bookings.Add(b);
            await ctx.SaveChangesAsync();
            return b.Id;
        });

        // Act
        await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IBookingRepository>();
            var booking = await repo.GetByIdAsync(bookingId, CancellationToken.None);
            repo.Delete(booking!);
            await repo.SaveChangesAsync(CancellationToken.None);
        });

        // Assert
        await ExecuteDbContextAsync(async ctx =>
        {
            var exists = await ctx.Bookings.AnyAsync(b => b.Id == bookingId);

            exists.Should().BeFalse();
        });
    }

    [Fact]
    public async Task GetByIdAsync_ExistEventId_ShouldRetrieveBookingByIdAndReturnEntity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var bookingId = await ExecuteDbContextAsync(async ctx =>
        {
            var b = Booking.Create(eventId, userId);
            ctx.Bookings.Add(b);
            await ctx.SaveChangesAsync();
            return b.Id;
        });

        // Act
        var result = await ExecuteScopeAsync(sp =>
            sp.GetRequiredService<IBookingRepository>()
            .GetByIdAsync(bookingId, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        bookingId.Should().Be(result.Id);
    }

    [Fact]
    public async Task GetPendingAsync_ShouldReturnOnlyPendingBookingIds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        await ExecuteDbContextAsync(async ctx =>
        {
            var b1 = Booking.Create(eventId, userId);
            var b2 = Booking.Create(eventId, userId);
            ctx.Bookings.AddRange(b1, b2);
            await ctx.SaveChangesAsync();
        });

        // Act
        var pendingIds = await ExecuteScopeAsync(sp =>
            sp.GetRequiredService<IBookingRepository>()
            .GetPendingAsync(CancellationToken.None));

        // Assert
        pendingIds.Should().HaveCount(2);

    }

    [Fact]
    public async Task Update_ShouldPersistBookingStatusChange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var bookingId = await ExecuteDbContextAsync(async ctx =>
        {
            var b = Booking.Create(eventId, userId);
            ctx.Bookings.Add(b);
            await ctx.SaveChangesAsync();
            return b.Id;
        });

        // Act
        await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IBookingRepository>();
            var booking = await repo.GetByIdAsync(bookingId, CancellationToken.None);
            booking!.Confirm();
            repo.Update(booking);
            await repo.SaveChangesAsync(CancellationToken.None);
        });

        // Assert
        await ExecuteDbContextAsync(async ctx =>
        {
            var updated = await ctx.Bookings.FindAsync([bookingId], CancellationToken.None);
            updated.Should().NotBeNull();
            updated.Status.Should().Be(BookingStatus.Confirmed);
            updated.ProcessedAt.Should().NotBeNull();
        });
    }
}
