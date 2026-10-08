using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Repositories;
using Events.Application.Commands;
using Events.Domain.Entities;
using Events.Domain.Enums;
using Events.Tests.Infrastructure;
using MediatR;
using Moq;

namespace Events.Tests.Commands;

public class BookingReservationRequestHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<ISeatReservationRepository> _reservationRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<ICorrelationContext> _correlationContextMock;
    private readonly BookingReservationRequestHandler _handler;

    public BookingReservationRequestHandlerTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _reservationRepositoryMock = new Mock<ISeatReservationRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();
        _correlationContextMock = new Mock<ICorrelationContext>();

        _handler = new BookingReservationRequestHandler(
            _eventRepositoryMock.Object,
            _reservationRepositoryMock.Object,
            _outboxServiceMock.Object,
            _correlationContextMock.Object);
    }

    [Fact]
    public async Task Handle_Should_AcceptReservationAndPublishAcceptedEvent_When_EventIsAvailable()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new BookingReservationRequestCommand(bookingId, eventId, userId, 2);

        var @event = TestEventFactory.Create(seats: 10, price: 500m, currency: "KZT");

        _reservationRepositoryMock
            .Setup(r => r.GetByBookingIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeatReservation?)null);

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        @event.AvailableSeats.Should().Be(8);

        _reservationRepositoryMock.Verify(r => r.Add(It.Is<SeatReservation>(s =>
            s.BookingId == bookingId &&
            s.EventId == eventId &&
            s.Seats == 2 &&
            s.UnitPrice == 500m &&
            s.TotalPrice == 1000m &&
            s.Status == SeatReservationStatus.Reserved)),
            Times.Once);

        _eventRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingReservationAccepted>(e =>
                e.BookingId == bookingId &&
                e.EventId == eventId &&
                e.Seats == 2 &&
                e.UnitPrice == 500m &&
                e.TotalPrice == 1000m),
            eventId.ToString()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Reject_When_EventNotFound()
    {
        // Arrange
        var command = new BookingReservationRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);

        _reservationRepositoryMock
            .Setup(r => r.GetByBookingIdAsync(command.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeatReservation?)null);

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingReservationRejected>(e =>
                e.BookingId == command.BookingId &&
                e.RejectionReason == ValidationFailureReason.EventNotFound),
            command.EventId.ToString()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Reject_When_EventIsInactive()
    {
        // Arrange
        var command = new BookingReservationRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        var @event = TestEventFactory.Create(seats: 10);
        @event.Cancel();

        _reservationRepositoryMock
            .Setup(r => r.GetByBookingIdAsync(command.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeatReservation?)null);

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingReservationRejected>(e =>
                e.BookingId == command.BookingId &&
                e.RejectionReason == ValidationFailureReason.EventNotFound),
            command.EventId.ToString()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Reject_When_SeatsNotAvailable()
    {
        // Arrange
        var command = new BookingReservationRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5);
        var @event = TestEventFactory.Create(seats: 2);

        _reservationRepositoryMock
            .Setup(r => r.GetByBookingIdAsync(command.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeatReservation?)null);

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        @event.AvailableSeats.Should().Be(2); // Unchanged

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingReservationRejected>(e =>
                e.BookingId == command.BookingId &&
                e.RejectionReason == ValidationFailureReason.SeatsNotAvailable),
            command.EventId.ToString()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_BeIdempotent_When_ReservationAlreadyReserved()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var command = new BookingReservationRequestCommand(bookingId, eventId, Guid.NewGuid(), 2);

        var existingReservation = SeatReservation.CreateReserved(
            bookingId, eventId, 2, 500m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), bookingId, Guid.NewGuid());

        _reservationRepositoryMock
            .Setup(r => r.GetByBookingIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReservation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _eventRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _reservationRepositoryMock.Verify(r => r.Add(It.IsAny<SeatReservation>()), Times.Never);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingReservationAccepted>(e =>
                e.BookingId == bookingId &&
                e.EventId == eventId),
            eventId.ToString()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_RejectWithoutDeductingSeats_When_CancelledBeforeReservationExists()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var command = new BookingReservationRequestCommand(bookingId, eventId, Guid.NewGuid(), 2);

        var existingReservation = SeatReservation.CreateCancelledBeforeReservation(
            bookingId, eventId, 2, correlationId: bookingId, causationId: Guid.NewGuid());

        _reservationRepositoryMock
            .Setup(r => r.GetByBookingIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReservation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _eventRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingReservationRejected>(e =>
                e.BookingId == bookingId),
            eventId.ToString()),
            Times.Once);
    }
}
