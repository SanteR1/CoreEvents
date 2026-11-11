using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Events;
using Events.Domain.DomainEvents;
using Moq;
using Xunit;

namespace Events.Tests.Handlers;

public class PublishEventCreatedHandlerTests
{
    private readonly Mock<IOutboxService> _outboxMock = new();
    private readonly PublishEventCreatedHandler _handler;

    public PublishEventCreatedHandlerTests()
    {
        _handler = new PublishEventCreatedHandler(_outboxMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldAddEventCreated_ToOutbox()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var startAt = DateTime.UtcNow.AddDays(1);
        var endAt = DateTime.UtcNow.AddDays(2);
        var now = DateTimeOffset.UtcNow;

        var domainEvent = new EventCreatedDomainEvent(
            EventId: eventId,
            Title: "Test Concert",
            Description: "Concert description",
            StartAt: startAt,
            EndAt: endAt,
            UnitPrice: 1500m,
            Currency: "KZT",
            PriceVersion: 1,
            TotalSeats: 250,
            IsActive: true,
            Version: 1,
            OccurredAt: now);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _outboxMock.Verify(o => o.AddIntegrationEvent(
            It.Is<EventCreated>(e =>
                e.EventId == eventId &&
                e.Title == "Test Concert" &&
                e.StartAt == startAt &&
                e.EndAt == endAt &&
                e.UnitPrice == 1500m &&
                e.Currency == "KZT" &&
                e.PriceVersion == 1 &&
                e.TotalSeats == 250 &&
                e.IsActive &&
                e.Version == 1 &&
                e.CreatedAt == now),
            eventId.ToString()),
            Times.Once);
    }
}
