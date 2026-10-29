using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Events;
using Events.Domain.DomainEvents;
using Moq;
using Xunit;

namespace Events.Tests.Handlers;

public class PublishBookingReservationAcceptedHandlerTests
{
    private readonly Mock<IOutboxService> _outboxMock = new();
    private readonly PublishBookingReservationAcceptedHandler _handler;

    public PublishBookingReservationAcceptedHandlerTests()
    {
        _handler = new PublishBookingReservationAcceptedHandler(_outboxMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldAddBookingReservationAccepted_ToOutbox()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var domainEvent = new SeatsReserved(eventId, bookingId, 3, 250.50m, "USD", now);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _outboxMock.Verify(o => o.AddIntegrationEvent(
            It.Is<BookingReservationAccepted>(e =>
                e.BookingId == bookingId &&
                e.EventId == eventId &&
                e.Seats == 3 &&
                e.UnitPrice == 250.50m &&
                e.TotalPrice == 751.50m &&
                e.Currency == "USD" &&
                e.ExpiresAt == now.AddMinutes(15)),
            eventId.ToString()),
            Times.Once);
    }
}
