using AwesomeAssertions;
using Events.Application.Abstractions;
using Events.Application.Events;
using Events.Domain.DomainEvents;
using Moq;
using Xunit;

namespace Events.Tests.Handlers;

public class InvalidateEventCacheHandlerTests
{
    private readonly Mock<IOutboxService> _outboxServiceMock = new();
    private readonly InvalidateEventCacheHandler _handler;

    public InvalidateEventCacheHandlerTests()
    {
        _handler = new InvalidateEventCacheHandler(_outboxServiceMock.Object);
    }

    [Fact]
    public async Task Handle_SeatsReleased_ShouldAddCacheInvalidationToOutbox()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var domainEvent = new SeatsReleased(eventId, Guid.NewGuid(), 2, DateTimeOffset.UtcNow);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _outboxServiceMock.Verify(x => x.AddCacheInvalidation(eventId), Times.Once);
    }

    [Fact]
    public async Task Handle_SeatsReserved_ShouldAddCacheInvalidationToOutbox()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var domainEvent = new SeatsReserved(eventId, Guid.NewGuid(), 2, 100m, "KZT", DateTimeOffset.UtcNow);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _outboxServiceMock.Verify(x => x.AddCacheInvalidation(eventId), Times.Once);
    }

    [Fact]
    public async Task Handle_EventCreated_ShouldAddCacheInvalidationToOutbox()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var domainEvent = new EventCreated(eventId, "Title", 100, DateTimeOffset.UtcNow);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _outboxServiceMock.Verify(x => x.AddCacheInvalidation(eventId), Times.Once);
    }

    [Fact]
    public async Task Handle_EventCancelled_ShouldAddCacheInvalidationToOutbox()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var domainEvent = new EventCancelled(eventId, DateTimeOffset.UtcNow);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _outboxServiceMock.Verify(x => x.AddCacheInvalidation(eventId), Times.Once);
    }
}
