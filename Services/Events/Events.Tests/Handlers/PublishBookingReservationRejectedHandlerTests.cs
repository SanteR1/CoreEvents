using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Events;
using Events.Domain.DomainEvents;
using Events.Domain.Enums;
using Moq;
using Xunit;

namespace Events.Tests.Handlers;

public class PublishBookingReservationRejectedHandlerTests
{
    private readonly Mock<IOutboxService> _outboxMock = new();
    private readonly PublishBookingReservationRejectedHandler _handler;

    public PublishBookingReservationRejectedHandlerTests()
    {
        _handler = new PublishBookingReservationRejectedHandler(_outboxMock.Object);
    }

    [Fact]
    public async Task Handle_WhenReasonIsEventInactive_ShouldAddRejectedWithEventNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var domainEvent = new SeatsReservationRejected(
            eventId, bookingId, 2, 0, SeatsReservationRejectionReason.EventInactive, DateTimeOffset.UtcNow);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _outboxMock.Verify(o => o.AddIntegrationEvent(
            It.Is<BookingReservationRejected>(e =>
                e.BookingId == bookingId &&
                e.EventId == eventId &&
                e.RejectionReason == ValidationFailureReason.EventNotFound),
            eventId.ToString()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenReasonIsInsufficientSeats_ShouldAddRejectedWithSeatsNotAvailable()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var domainEvent = new SeatsReservationRejected(
            eventId, bookingId, 5, 2, SeatsReservationRejectionReason.InsufficientSeats, DateTimeOffset.UtcNow);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _outboxMock.Verify(o => o.AddIntegrationEvent(
            It.Is<BookingReservationRejected>(e =>
                e.BookingId == bookingId &&
                e.EventId == eventId &&
                e.RejectionReason == ValidationFailureReason.SeatsNotAvailable),
            eventId.ToString()),
            Times.Once);
    }
}
