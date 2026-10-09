using AwesomeAssertions;
using Bookings.Domain.Entities;

namespace Bookings.Tests.Domain;

public class EventProjectionTests
{
    [Fact]
    public void Create_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var updatedAt = DateTimeOffset.UtcNow;

        // Act
        var projection = EventProjection.Create(
            id, "Title", now, now.AddDays(1), 500m, "KZT", 1L, 100, true, 1L, updatedAt);

        // Assert
        projection.Id.Should().Be(id);
        projection.Title.Should().Be("Title");
        projection.StartAt.Should().Be(now);
        projection.EndAt.Should().Be(now.AddDays(1));
        projection.UnitPrice.Should().Be(500m);
        projection.Currency.Should().Be("KZT");
        projection.PriceVersion.Should().Be(1L);
        projection.TotalSeats.Should().Be(100);
        projection.IsActive.Should().BeTrue();
        projection.Version.Should().Be(1L);
        projection.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void Update_WhenNewerVersion_ShouldApplyChanges()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var projection = EventProjection.Create(
            Guid.NewGuid(), "Old Title", now, now.AddDays(1), 500m, "KZT", 1L, 100, true, 1L, DateTimeOffset.UtcNow);

        // Act
        projection.Update("New Title", now.AddDays(1), now.AddDays(2), 600m, "EUR", 2L, 150, true, version: 2L, DateTimeOffset.UtcNow);

        // Assert
        projection.Title.Should().Be("New Title");
        projection.UnitPrice.Should().Be(600m);
        projection.Currency.Should().Be("EUR");
        projection.PriceVersion.Should().Be(2L);
        projection.TotalSeats.Should().Be(150);
        projection.Version.Should().Be(2L);
    }

    [Fact]
    public void Update_WhenOlderOrSameVersion_ShouldIgnoreOutOfOrderMessage()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var projection = EventProjection.Create(
            Guid.NewGuid(), "Fresh Title", now, now.AddDays(1), 500m, "KZT", 2L, 100, true, version: 3L, DateTimeOffset.UtcNow);

        // Act (Simulate out-of-order message with version 2)
        projection.Update("Stale Title", now, now.AddDays(1), 100m, "KZT", 1L, 50, true, version: 2L, DateTimeOffset.UtcNow);

        // Assert
        projection.Title.Should().Be("Fresh Title");
        projection.UnitPrice.Should().Be(500m);
        projection.Version.Should().Be(3L);
    }

    [Fact]
    public void Cancel_WhenValidVersion_ShouldDeactivateProjection()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var projection = EventProjection.Create(
            Guid.NewGuid(), "Title", now, now.AddDays(1), 500m, "KZT", 1L, 100, true, version: 1L, DateTimeOffset.UtcNow);

        // Act
        projection.Cancel(version: 2L, DateTimeOffset.UtcNow);

        // Assert
        projection.IsActive.Should().BeFalse();
        projection.Version.Should().Be(2L);
    }

    [Fact]
    public void Cancel_WhenOlderVersion_ShouldIgnore()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var projection = EventProjection.Create(
            Guid.NewGuid(), "Title", now, now.AddDays(1), 500m, "KZT", 1L, 100, true, version: 3L, DateTimeOffset.UtcNow);

        // Act
        projection.Cancel(version: 1L, DateTimeOffset.UtcNow);

        // Assert
        projection.IsActive.Should().BeTrue();
        projection.Version.Should().Be(3L);
    }

    [Fact]
    public void Create_WhenCurrencyEmpty_ShouldFallbackToKZT()
    {
        var now = DateTime.UtcNow;
        var projection = EventProjection.Create(
            Guid.NewGuid(), "Title", now, now.AddDays(1), 500m, "", 1L, 100, true, version: 1L, DateTimeOffset.UtcNow);

        projection.Currency.Should().Be("KZT");
    }
}
