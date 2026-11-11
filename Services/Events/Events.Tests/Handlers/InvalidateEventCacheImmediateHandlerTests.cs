using AwesomeAssertions;
using Events.Application.Abstractions.Caching;
using Events.Application.Events;
using Events.Domain.DomainEvents;
using Moq;
using Xunit;

namespace Events.Tests.Handlers;

public class InvalidateEventCacheImmediateHandlerTests
{
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly InvalidateEventCacheImmediateHandler _handler;

    public InvalidateEventCacheImmediateHandlerTests()
    {
        _handler = new InvalidateEventCacheImmediateHandler(_cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_SeatsReleased_ShouldDeleteDirectlyFromCache()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var domainEvent = new SeatsReleasedDomainEvent(eventId, Guid.NewGuid(), 2, DateTimeOffset.UtcNow);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(x => x.DeleteAsync(CacheKeys.Event(eventId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SeatsReserved_ShouldDeleteDirectlyFromCache()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var domainEvent = new SeatsReservedDomainEvent(eventId, Guid.NewGuid(), 2, 100m, "KZT", DateTimeOffset.UtcNow);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(x => x.DeleteAsync(CacheKeys.Event(eventId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EventCreated_ShouldDeleteDirectlyFromCache()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var domainEvent = new EventCreatedDomainEvent(
            EventId: eventId,
            Title: "Title",
            Description: null,
            StartAt: DateTime.UtcNow.AddDays(1),
            EndAt: DateTime.UtcNow.AddDays(2),
            UnitPrice: 1000m,
            Currency: "KZT",
            PriceVersion: 1,
            TotalSeats: 100,
            IsActive: true,
            Version: 1,
            OccurredAt: now);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(x => x.DeleteAsync(CacheKeys.Event(eventId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EventCancelled_ShouldDeleteDirectlyFromCache()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var domainEvent = new EventCancelledDomainEvent(eventId, 1L, DateTimeOffset.UtcNow);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(x => x.DeleteAsync(CacheKeys.Event(eventId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EventUpdated_ShouldDeleteDirectlyFromCache()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var domainEvent = new EventUpdatedDomainEvent(
            EventId: eventId,
            Title: "Updated",
            Description: null,
            StartAt: DateTime.UtcNow.AddDays(1),
            EndAt: DateTime.UtcNow.AddDays(2),
            UnitPrice: 1000m,
            Currency: "KZT",
            PriceVersion: 2,
            TotalSeats: 100,
            IsActive: true,
            Version: 2,
            OccurredAt: now);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(x => x.DeleteAsync(CacheKeys.Event(eventId), It.IsAny<CancellationToken>()), Times.Once);
    }
}
