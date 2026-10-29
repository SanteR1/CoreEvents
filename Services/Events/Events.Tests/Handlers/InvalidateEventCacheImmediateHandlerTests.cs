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
        var domainEvent = new SeatsReleased(eventId, Guid.NewGuid(), 2, DateTimeOffset.UtcNow);

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
        var domainEvent = new SeatsReserved(eventId, Guid.NewGuid(), 2, 100m, "KZT", DateTimeOffset.UtcNow);

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
        var domainEvent = new EventCreated(eventId, "Title", 100, DateTimeOffset.UtcNow);

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
        var domainEvent = new EventCancelled(eventId, DateTimeOffset.UtcNow);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(x => x.DeleteAsync(CacheKeys.Event(eventId), It.IsAny<CancellationToken>()), Times.Once);
    }
}
