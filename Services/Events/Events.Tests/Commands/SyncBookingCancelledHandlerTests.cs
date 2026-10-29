using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Exceptions;
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

public class SyncBookingCancelledHandlerTests
{
    private readonly Mock<IEventRepository> _eventsMock;
    private readonly Mock<ISeatReservationRepository> _reservationsMock;
    private readonly SyncBookingCancelledHandler _handler;

    public SyncBookingCancelledHandlerTests()
    {
        _eventsMock = new Mock<IEventRepository>();
        _reservationsMock = new Mock<ISeatReservationRepository>();
        _handler = new SyncBookingCancelledHandler(
            _eventsMock.Object,
            _reservationsMock.Object,
            NullLogger<SyncBookingCancelledHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenReservationNotFound_ShouldCompleteAsNoOp_WithoutSaving()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _reservationsMock.Setup(r => r.GetByBookingIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeatReservation?)null);

        var command = new SyncBookingCancelledCommand(bookingId, eventId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(InboxProcessingResult.Processed);
        _reservationsMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _eventsMock.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReservationIsAlreadyReleased_ShouldCompleteAsNoOp_WithoutSaving()
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

        var command = new SyncBookingCancelledCommand(reservation.BookingId, @event.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(InboxProcessingResult.Processed);
        _reservationsMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReservationIsRejected_ShouldCompleteAsNoOp_WithoutSaving()
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

        var command = new SyncBookingCancelledCommand(reservation.BookingId, @event.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(InboxProcessingResult.Processed);
        _reservationsMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReservationIsReserved_AndEventMissing_ShouldThrowDataIntegrityException_WithoutSaving()
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

        var command = new SyncBookingCancelledCommand(bookingId, eventId);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DataIntegrityException>();
        _reservationsMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenIntegrityViolationOccurs_ShouldLogCriticalError_AndThrowDataIntegrityException()
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
            .ReturnsAsync((Event?)null); // Event missing -> EventMissing violation

        var testLogger = new TestLogger<SyncBookingCancelledHandler>();
        var handlerWithLogger = new SyncBookingCancelledHandler(
            _eventsMock.Object,
            _reservationsMock.Object,
            testLogger);

        var command = new SyncBookingCancelledCommand(bookingId, eventId);

        // Act
        Func<Task> act = async () => await handlerWithLogger.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DataIntegrityException>();
        testLogger.Logs.Should().Contain(
            l => l.Level == Microsoft.Extensions.Logging.LogLevel.Error &&
                 l.Message.Contains("Integrity violation"),
            "Хендлер обязан логировать критическую ошибку LogError для срабатывания мониторинга и алертов");
    }

    [Fact]
    public async Task Handle_WhenReservationIsReserved_AndEventReleaseSeatsFails_ShouldThrowDataIntegrityException_WithoutSaving()
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

        var command = new SyncBookingCancelledCommand(reservation.BookingId, @event.Id);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DataIntegrityException>();
        _reservationsMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReservationIsReserved_AndNormalCase_ShouldReleaseSeats_WithoutManualSave()
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

        var command = new SyncBookingCancelledCommand(reservation.BookingId, @event.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(InboxProcessingResult.Processed);
        reservation.Status.Should().Be(SeatsReservationStatus.Released);
        @event.AvailableSeats.Should().Be(10);
        _reservationsMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never, "Сохранение возложено на TransactionBehavior");
    }
}
