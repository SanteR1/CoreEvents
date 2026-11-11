using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Events;
using Events.Domain.DomainEvents;
using Moq;
using Xunit;

namespace Events.Tests.Handlers;

public class PublishEventUpdatedHandlerTests
{
    private readonly Mock<IOutboxService> _outboxMock = new();
    private readonly PublishEventUpdatedHandler _handler;

    public PublishEventUpdatedHandlerTests()
    {
        _handler = new PublishEventUpdatedHandler(_outboxMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldAddEventUpdated_ToOutbox()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var startAt = DateTime.UtcNow.AddDays(2);
        var endAt = DateTime.UtcNow.AddDays(3);
        var now = DateTimeOffset.UtcNow;

        var domainEvent = new EventUpdatedDomainEvent(
            EventId: eventId,
            Title: "Updated Title",
            Description: "Updated Description",
            StartAt: startAt,
            EndAt: endAt,
            UnitPrice: 2000m,
            Currency: "USD",
            PriceVersion: 2,
            TotalSeats: 300,
            IsActive: true,
            Version: 2,
            OccurredAt: now);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _outboxMock.Verify(o => o.AddIntegrationEvent(
            It.Is<EventUpdated>(e =>
                e.EventId == eventId &&
                e.Title == "Updated Title" &&
                e.Description == "Updated Description" &&
                e.StartAt == startAt &&
                e.EndAt == endAt &&
                e.UnitPrice == 2000m &&
                e.Currency == "USD" &&
                e.PriceVersion == 2 &&
                e.TotalSeats == 300 &&
                e.IsActive &&
                e.Version == 2 &&
                e.UpdatedAt == now),
            eventId.ToString()),
            Times.Once);
    }
}
