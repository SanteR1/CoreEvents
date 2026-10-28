using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Exceptions;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Repositories;
using Events.Application.Commands;
using Events.Domain.Entities;
using Events.Domain.Enums;
using Events.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Events.Tests.Commands;

public class ReleaseSeatsHandlerTests
{
    private readonly Mock<IEventRepository> _eventsMock;
    private readonly Mock<ISeatReservationRepository> _reservationsMock;
    private readonly Mock<IOutboxService> _outboxMock;
    private readonly ReleaseSeatsHandler _handler;

    public ReleaseSeatsHandlerTests()
    {
        _eventsMock = new Mock<IEventRepository>();
        _reservationsMock = new Mock<ISeatReservationRepository>();
        _outboxMock = new Mock<IOutboxService>();
        _handler = new ReleaseSeatsHandler(
            _eventsMock.Object,
            _reservationsMock.Object,
            _outboxMock.Object,
            NullLogger<ReleaseSeatsHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenReservationIsNull_ShouldAddIntegrationEventSeatsReleasedFalse_WithoutSaving()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _reservationsMock.Setup(r => r.GetByBookingIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeatReservation?)null);

        var command = new ReleaseSeatsCommand(bookingId, eventId, 3);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(InboxProcessingResult.Processed);
        _outboxMock.Verify(o => o.AddIntegrationEvent(
            It.Is<EventBookingCancellationCompleted>(e =>
                e.BookingId == bookingId &&
                e.EventId == eventId &&
                !e.SeatsReleased),
            eventId.ToString()),
            Times.Once);
        _eventsMock.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReservationIsAlreadyReleased_ShouldAddIntegrationEventSeatsReleasedTrue_WithoutSaving()
    {
        // Arrange
        var @event = TestEventFactory.Create(seats: 10);
        @event.TryReserveSeats(2);
        var reservation = SeatReservation.CreateReserved(
            Guid.NewGuid(), @event.Id, 2, 100m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());
        reservation.Release(); // Status = Released

        _reservationsMock.Setup(r => r.GetByBookingIdAsync(reservation.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _eventsMock.Setup(e => e.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new ReleaseSeatsCommand(reservation.BookingId, @event.Id, 2);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(InboxProcessingResult.Processed);
        _outboxMock.Verify(o => o.AddIntegrationEvent(
            It.Is<EventBookingCancellationCompleted>(e =>
                e.BookingId == reservation.BookingId &&
                e.EventId == @event.Id &&
                e.SeatsReleased),
            @event.Id.ToString()),
            Times.Once);
        _eventsMock.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReservationIsRejected_ShouldAddIntegrationEventSeatsReleasedFalse_WithoutSaving()
    {
        // Arrange
        var @event = TestEventFactory.Create(seats: 10);
        var reservation = SeatReservation.CreateRejected(
            Guid.NewGuid(), @event.Id, 2, ValidationFailureReason.SeatsNotAvailable,
            Guid.NewGuid(), Guid.NewGuid());

        _reservationsMock.Setup(r => r.GetByBookingIdAsync(reservation.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _eventsMock.Setup(e => e.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new ReleaseSeatsCommand(reservation.BookingId, @event.Id, 2);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(InboxProcessingResult.Processed);
        _outboxMock.Verify(o => o.AddIntegrationEvent(
            It.Is<EventBookingCancellationCompleted>(e =>
                e.BookingId == reservation.BookingId &&
                e.EventId == @event.Id &&
                !e.SeatsReleased),
            @event.Id.ToString()),
            Times.Once);
        _eventsMock.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReservationIsReserved_AndEventMissing_ShouldThrowDataIntegrityException_WithoutPublishOrSave()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var reservation = SeatReservation.CreateReserved(
            bookingId, eventId, 3, 100m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());

        _reservationsMock.Setup(r => r.GetByBookingIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _eventsMock.Setup(e => e.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var command = new ReleaseSeatsCommand(bookingId, eventId, 3);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DataIntegrityException>();
        _outboxMock.Verify(o => o.AddIntegrationEvent(It.IsAny<EventBookingCancellationCompleted>(), It.IsAny<string>()), Times.Never);
        _eventsMock.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReservationIsReserved_AndEventReleaseSeatsFails_ShouldThrowDataIntegrityException_WithoutPublishOrSave()
    {
        // Arrange: available 10 out of 10, releasing 5 would exceed total seats
        var @event = TestEventFactory.Create(seats: 10);
        var reservation = SeatReservation.CreateReserved(
            Guid.NewGuid(), @event.Id, 5, 100m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());

        _reservationsMock.Setup(r => r.GetByBookingIdAsync(reservation.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _eventsMock.Setup(e => e.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new ReleaseSeatsCommand(reservation.BookingId, @event.Id, 5);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DataIntegrityException>();
        _outboxMock.Verify(o => o.AddIntegrationEvent(It.IsAny<EventBookingCancellationCompleted>(), It.IsAny<string>()), Times.Never);
        _eventsMock.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReservationIsReserved_AndNormalCase_ShouldReleaseSeats_AddIntegrationEventSeatsReleasedTrue_WithoutManualSave()
    {
        // Arrange
        var @event = TestEventFactory.Create(seats: 10);
        @event.TryReserveSeats(4);
        var reservation = SeatReservation.CreateReserved(
            Guid.NewGuid(), @event.Id, 4, 100m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());

        _reservationsMock.Setup(r => r.GetByBookingIdAsync(reservation.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _eventsMock.Setup(e => e.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new ReleaseSeatsCommand(reservation.BookingId, @event.Id, 4);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(InboxProcessingResult.Processed);
        reservation.Status.Should().Be(SeatsReservationStatus.Released);
        @event.AvailableSeats.Should().Be(10);
        _outboxMock.Verify(o => o.AddIntegrationEvent(
            It.Is<EventBookingCancellationCompleted>(e =>
                e.BookingId == reservation.BookingId &&
                e.EventId == @event.Id &&
                e.SeatsReleased),
            @event.Id.ToString()),
            Times.Once);
        _eventsMock.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never, "Сохранение возложено на TransactionBehavior");
    }
}
