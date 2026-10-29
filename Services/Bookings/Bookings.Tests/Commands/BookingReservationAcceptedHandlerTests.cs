using AwesomeAssertions;
using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Commands;
using Bookings.Domain.Entities;
using Bookings.Domain.Enums;
using CoreEvents.Shared.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Bookings.Tests.Commands;

public class BookingReservationAcceptedHandlerTests
{
    private readonly Mock<IBookingRepository> _repositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<ILogger<BookingReservationAcceptedHandler>> _loggerMock;
    private readonly BookingReservationAcceptedHandler _handler;

    public BookingReservationAcceptedHandlerTests()
    {
        _repositoryMock = new Mock<IBookingRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();
        _loggerMock = new Mock<ILogger<BookingReservationAcceptedHandler>>();

        _handler = new BookingReservationAcceptedHandler(
            _repositoryMock.Object,
            _outboxServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ConfirmBookingAndPublishBookingConfirmed_When_BookingIsPending()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), 2);
        var command = new BookingReservationAcceptedCommand(
            booking.Id, booking.EventId, 2, 500m, 1000m, "KZT", 0m, 1L, DateTimeOffset.UtcNow.AddMinutes(15));

        _repositoryMock
            .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.UnitPrice.Should().Be(500m);
        booking.TotalPrice.Should().Be(1000m);

        _repositoryMock.Verify(r => r.Update(booking), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingConfirmed>(e =>
                e.BookingId == booking.Id &&
                e.EventId == booking.EventId &&
                e.UserId == booking.UserId &&
                e.Seats == booking.Seats),
            booking.EventId.ToString()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_CompensateAndPublishReleaseRequested_When_BookingIsAlreadyRejected()
    {
        // Arrange (Late Arrival: reservation accepted after booking timed out or was rejected)
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), 2);
        booking.Reject(ValidationFailureReason.Timeout);

        var command = new BookingReservationAcceptedCommand(
            booking.Id, booking.EventId, 2, 500m, 1000m, "KZT", 0m, 1L, DateTimeOffset.UtcNow.AddMinutes(15));

        _repositoryMock
            .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        booking.Status.Should().Be(BookingStatus.Rejected); // Not altered!

        // Must issue compensating release request
        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingReservationReleaseRequested>(e =>
                e.BookingId == booking.Id &&
                e.EventId == booking.EventId &&
                e.Seats == 2 &&
                e.Reason == CancellationReason.Timeout),
            booking.EventId.ToString()),
            Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_CompensateAndPublishReleaseRequested_When_BookingIsAlreadyCancelled()
    {
        // Arrange (Late Arrival: reservation accepted after booking was cancelled)
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), 2);
        booking.Cancel();

        var command = new BookingReservationAcceptedCommand(
            booking.Id, booking.EventId, 2, 500m, 1000m, "KZT", 0m, 1L, DateTimeOffset.UtcNow.AddMinutes(15));

        _repositoryMock
            .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        booking.Status.Should().Be(BookingStatus.Cancelled);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingReservationReleaseRequested>(e =>
                e.BookingId == booking.Id &&
                e.EventId == booking.EventId &&
                e.Seats == 2),
            booking.EventId.ToString()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_BeIdempotent_When_BookingIsAlreadyConfirmed()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), 2);
        booking.Confirm(500m, 1000m, "KZT");

        var command = new BookingReservationAcceptedCommand(
            booking.Id, booking.EventId, 2, 500m, 1000m, "KZT", 0m, 1L, DateTimeOffset.UtcNow.AddMinutes(15));

        _repositoryMock
            .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Booking>()), Times.Never);
        _outboxServiceMock.Verify(o => o.Publish(It.IsAny<BookingConfirmed>(), It.IsAny<string>()), Times.Never);
        _outboxServiceMock.Verify(o => o.Publish(It.IsAny<BookingReservationReleaseRequested>(), It.IsAny<string>()), Times.Never);
    }
}
