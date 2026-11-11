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
        var domainEvent = new SeatsReleasedDomainEvent(eventId, Guid.NewGuid(), 2, DateTimeOffset.UtcNow);

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
        var domainEvent = new SeatsReservedDomainEvent(eventId, Guid.NewGuid(), 2, 100m, "KZT", DateTimeOffset.UtcNow);

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
        _outboxServiceMock.Verify(x => x.AddCacheInvalidation(eventId), Times.Once);
    }

    [Fact]
    public async Task Handle_EventCancelled_ShouldAddCacheInvalidationToOutbox()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var domainEvent = new EventCancelledDomainEvent(eventId, 1L, DateTimeOffset.UtcNow);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _outboxServiceMock.Verify(x => x.AddCacheInvalidation(eventId), Times.Once);
    }

    [Fact]
    public async Task Handle_EventUpdated_ShouldAddCacheInvalidationToOutbox()
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
        _outboxServiceMock.Verify(x => x.AddCacheInvalidation(eventId), Times.Once);
    }
}
