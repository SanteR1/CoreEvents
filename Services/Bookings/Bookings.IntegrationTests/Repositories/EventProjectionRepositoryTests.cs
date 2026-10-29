using AwesomeAssertions;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Domain.Entities;
using Bookings.IntegrationTests.Infrastructure.Bases;
using Bookings.IntegrationTests.Infrastructure.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bookings.IntegrationTests.Repositories;

public class EventProjectionRepositoryTests(ApiOnlyIntegrationTestFactory factory) : ApiOnlyIntegrationTestBase(factory)
{
    [Fact]
    public async Task AddAsync_AndSave_ShouldPersistProjection()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var projection = EventProjection.Create(
            id: eventId,
            title: "Projection Test",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(1).AddHours(2),
            unitPrice: 750m,
            currency: "KZT",
            priceVersion: 1L,
            totalSeats: 100);

        // Act
        await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IEventProjectionRepository>();
            await repo.AddAsync(projection);
            await repo.SaveChangesAsync();
        });

        // Assert
        await ExecuteDbContextAsync(async ctx =>
        {
            var inDb = await ctx.EventProjections.FirstOrDefaultAsync(p => p.Id == eventId);
            inDb.Should().NotBeNull();
            inDb.Title.Should().Be("Projection Test");
            inDb.UnitPrice.Should().Be(750m);
            inDb.Currency.Should().Be("KZT");
            inDb.IsActive.Should().BeTrue();
        });
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ShouldReturnProjection()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var projection = EventProjection.Create(
            id: eventId,
            title: "Projection Test 2",
            startAt: DateTime.UtcNow.AddDays(2),
            endAt: DateTime.UtcNow.AddDays(2).AddHours(2),
            unitPrice: 1000m,
            currency: "KZT",
            priceVersion: 1L,
            totalSeats: 50);

        await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IEventProjectionRepository>();
            await repo.AddAsync(projection);
            await repo.SaveChangesAsync();
        });

        // Act
        var result = await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IEventProjectionRepository>();
            return await repo.GetByIdAsync(eventId);
        });

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(eventId);
        result.UnitPrice.Should().Be(1000m);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ShouldReturnNull()
    {
        // Act
        var result = await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IEventProjectionRepository>();
            return await repo.GetByIdAsync(Guid.NewGuid());
        });

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_ShouldPersistProjectionChanges()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var projection = EventProjection.Create(
            id: eventId,
            title: "Initial Title",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(1).AddHours(2),
            unitPrice: 500m,
            currency: "KZT",
            priceVersion: 1L,
            totalSeats: 50);

        await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IEventProjectionRepository>();
            await repo.AddAsync(projection);
            await repo.SaveChangesAsync();
        });

        // Act
        await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IEventProjectionRepository>();
            var existing = await repo.GetByIdAsync(eventId);
            existing.Should().NotBeNull();
            existing.Update(
                title: "Updated Title",
                startAt: DateTime.UtcNow.AddDays(2),
                endAt: DateTime.UtcNow.AddDays(2).AddHours(2),
                unitPrice: 800m,
                currency: "KZT",
                priceVersion: 2L,
                totalSeats: 60,
                isActive: true,
                version: 2L,
                updatedAt: DateTimeOffset.UtcNow);
            repo.Update(existing);
            await repo.SaveChangesAsync();
        });

        // Assert
        await ExecuteDbContextAsync(async ctx =>
        {
            var inDb = await ctx.EventProjections.FindAsync(eventId);
            inDb.Should().NotBeNull();
            inDb.Title.Should().Be("Updated Title");
            inDb.UnitPrice.Should().Be(800m);
            inDb.PriceVersion.Should().Be(2L);
            inDb.Version.Should().Be(2L);
        });
    }
}
