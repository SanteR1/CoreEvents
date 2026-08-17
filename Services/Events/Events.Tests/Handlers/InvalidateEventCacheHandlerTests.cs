using Events.Application.Abstractions.Caching;
using Events.Application.Abstractions.Messaging;
using Events.Application.Events;
using Events.Domain.DomainEvents;
using Moq;

namespace Events.Tests.Handlers;

public class InvalidateEventCacheHandlerTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly InvalidateEventCacheHandler _handler;

    public InvalidateEventCacheHandlerTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _handler = new InvalidateEventCacheHandler(_cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldDeleteCache_WhenEventImplementsICacheInvalidationEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var domainEvent = new SeatsReleased(eventId);
        var notification = new DomainEventNotification(domainEvent);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(
            x => x.DeleteAsync($"events:{eventId}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldDoNothing_WhenEventIsNotRelatedToCache()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var irrelevantEvent = new EventCreated(eventId);
        var notification = new DomainEventNotification(irrelevantEvent);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(
            x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
