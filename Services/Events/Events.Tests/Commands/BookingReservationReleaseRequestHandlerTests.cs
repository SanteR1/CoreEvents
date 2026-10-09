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

public class BookingReservationReleaseRequestHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<ISeatReservationRepository> _reservationRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<ICorrelationContext> _correlationContextMock;
    private readonly BookingReservationReleaseRequestHandler _handler;

    public BookingReservationReleaseRequestHandlerTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _reservationRepositoryMock = new Mock<ISeatReservationRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();
        _correlationContextMock = new Mock<ICorrelationContext>();

        _handler = new BookingReservationReleaseRequestHandler(
            _eventRepositoryMock.Object,
            _reservationRepositoryMock.Object,
            _outboxServiceMock.Object,
            _correlationContextMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReleaseSeatsAndPublishReleased_When_ReservationIsReserved()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var command = new BookingReservationReleaseRequestCommand(bookingId, eventId, 2, CancellationReason.UserCancelled);

        var @event = TestEventFactory.Create(seats: 10);
        @event.TryReserveSeats(2); // 8 available now

        var reservation = SeatReservation.CreateReserved(
            bookingId, eventId, 2, 100m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), bookingId, Guid.NewGuid());

        _reservationRepositoryMock
            .Setup(r => r.GetByBookingIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        reservation.Status.Should().Be(SeatReservationStatus.Released);
        @event.AvailableSeats.Should().Be(10); // Restored!

        _reservationRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingReservationReleased>(e =>
                e.BookingId == bookingId &&
                e.EventId == eventId &&
                e.Seats == 2),
            eventId.ToString()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_CreateTombstoneAndPublishReleased_When_ReservationDoesNotExist()
    {
        // Arrange (Early cancellation arrives before reservation)
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var command = new BookingReservationReleaseRequestCommand(bookingId, eventId, 3, CancellationReason.UserCancelled);

        _reservationRepositoryMock
            .Setup(r => r.GetByBookingIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeatReservation?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _reservationRepositoryMock.Verify(r => r.Add(It.Is<SeatReservation>(s =>
            s.BookingId == bookingId &&
            s.EventId == eventId &&
            s.Seats == 3 &&
            s.Status == SeatReservationStatus.CancelledBeforeReservation)),
            Times.Once);

        _reservationRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingReservationReleased>(e =>
                e.BookingId == bookingId &&
                e.EventId == eventId &&
                e.Seats == 3),
            eventId.ToString()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_BeIdempotentAndPublishReleased_When_AlreadyReleased()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var command = new BookingReservationReleaseRequestCommand(bookingId, eventId, 2, CancellationReason.UserCancelled);

        var reservation = SeatReservation.CreateReserved(
            bookingId, eventId, 2, 100m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), bookingId, Guid.NewGuid());
        reservation.Release(); // Already released

        _reservationRepositoryMock
            .Setup(r => r.GetByBookingIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _eventRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _reservationRepositoryMock.Verify(r => r.Update(It.IsAny<SeatReservation>()), Times.Never);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingReservationReleased>(e =>
                e.BookingId == bookingId &&
                e.EventId == eventId),
            eventId.ToString()),
            Times.Once);
    }
}
