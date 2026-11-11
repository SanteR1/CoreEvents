using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Events;
using Events.Domain.DomainEvents;
using Moq;
using Xunit;

namespace Events.Tests.Handlers;

public class PublishEventCancelledHandlerTests
{
    private readonly Mock<IOutboxService> _outboxMock = new();
    private readonly PublishEventCancelledHandler _handler;

    public PublishEventCancelledHandlerTests()
    {
        _handler = new PublishEventCancelledHandler(_outboxMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldAddEventCancelled_ToOutbox()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var domainEvent = new EventCancelledDomainEvent(eventId, 2L, now);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _outboxMock.Verify(o => o.AddIntegrationEvent(
            It.Is<EventCancelled>(e =>
                e.EventId == eventId &&
                e.Version == 2L &&
                e.CancelledAt == now),
            eventId.ToString()),
            Times.Once);
    }
}

