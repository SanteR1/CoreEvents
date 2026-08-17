using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Commands;
using Bookings.Application.Exceptions;
using Bookings.Domain.Entities;
using Bookings.Domain.Exceptions;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Identity.Enums;
using FluentAssertions;
using Moq;

namespace Bookings.Tests.Commands;

public class CancelBookingHandlerTests
{
    private readonly Mock<IBookingRepository> _repositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly CancelBookingHandler _handler;

    public CancelBookingHandlerTests()
    {
        _repositoryMock = new Mock<IBookingRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();

        _handler = new CancelBookingHandler(_repositoryMock.Object, _outboxServiceMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowBookingNotFoundException_When_BookingDoesNotExist()
    {
        // Arrange
        var command = new CancelBookingByUserCommand(Guid.NewGuid(), Guid.NewGuid(), RoleName.User);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(command.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BookingNotFoundException>()
                 .WithMessage($"*{command.BookingId}*");

        _outboxServiceMock.Verify(o => o.Publish(It.IsAny<BookingCancellationRequested>(), It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowException_When_UserIsNotAdminAndNotOwner()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var hackerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var command = new CancelBookingByUserCommand(Guid.NewGuid(), hackerId, RoleName.User);

        var booking = Booking.Create(eventId, ownerId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(command.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotBookingOwnerException>();

        _outboxServiceMock.Verify(o => o.Publish(It.IsAny<BookingCancellationRequested>(), It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_PublishEventWithUserCancelledReason_When_CancelledByOwner()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var seats = 2;

        var command = new CancelBookingByUserCommand(Guid.NewGuid(), ownerId, RoleName.User);
        var booking = Booking.Create(eventId, ownerId, seats);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(command.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(booking.Id);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingCancellationRequested>(e =>
                e.BookingId == booking.Id &&
                e.EventId == booking.EventId &&
                e.UserId == booking.UserId &&
                e.Reason == CancellationReason.UserCancelled &&
                e.CancelledAt <= DateTimeOffset.UtcNow),
            booking.EventId.ToString()),
            Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_PublishEventWithAdminCancelledReason_When_CancelledByAdmin()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var command = new CancelBookingByUserCommand(Guid.NewGuid(), adminId, RoleName.Admin);

        var booking = Booking.Create(eventId, ownerId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(command.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(booking.Id);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingCancellationRequested>(e =>
                e.BookingId == booking.Id &&
                e.EventId == booking.EventId &&
                e.UserId == booking.UserId &&
                e.Reason == CancellationReason.AdminCancelled),
            booking.EventId.ToString()),
            Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
