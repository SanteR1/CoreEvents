using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Commands;
using Bookings.Application.Configuration;
using Bookings.Application.Exceptions;
using Bookings.Domain.Entities;
using CoreEvents.Shared.Contracts.Events;
using FluentAssertions;
using Moq;

namespace Bookings.Tests.Commands;

public class CreateBookingHandlerTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly BookingSettings _bookingSettings;
    private readonly CreateBookingHandler _handler;

    public CreateBookingHandlerTests()
    {
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();
        _bookingSettings = new BookingSettings { MaxBookingsPerUser = 10 };

        _handler = new CreateBookingHandler(
            _bookingRepositoryMock.Object,
            _outboxServiceMock.Object,
            _bookingSettings);
    }

    [Fact]
    public async Task Handle_Should_CreateBookingAndPublishToOutbox_When_LimitIsNotExceeded()
    {
        // Arrange
        var command = new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid(), 12);

        _bookingRepositoryMock
            .Setup(repo => repo.GetBookingCountForUserAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();

        _bookingRepositoryMock.Verify(
            repo => repo.Add(It.Is<Booking>(b =>
                b.EventId == command.EventId &&
                b.UserId == command.UserId &&
                b.Seats == command.Seats)),
            Times.Once);

        _outboxServiceMock.Verify(o => o.Publish(
                It.Is<BookingConfirmed>(e =>
                    e.BookingId == result.Id &&
                    e.EventId == command.EventId &&
                    e.UserId == command.UserId &&
                    e.Seats == command.Seats),
                command.EventId.ToString()),
            Times.Once);

        _bookingRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_SetSeatsToOne_When_SeatsIsNull()
    {
        // Arrange
        var command = new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid(), Seats: null);

        _bookingRepositoryMock
            .Setup(r => r.GetBookingCountForUserAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _outboxServiceMock.Verify(o => o.Publish(
                It.Is<BookingConfirmed>(e => e.Seats == 1),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowActiveBookingLimitExceededException_When_UserIsAtLimit()
    {
        // Arrange
        var command = new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid());

        _bookingRepositoryMock
            .Setup(r => r.GetBookingCountForUserAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(11);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ActiveBookingLimitExceededException>()
                 .WithMessage($"*{_bookingSettings.MaxBookingsPerUser}*");

        _bookingRepositoryMock.Verify(r => r.Add(It.IsAny<Booking>()), Times.Never);
        _outboxServiceMock.Verify(o => o.Publish(It.IsAny<BookingConfirmed>(), It.IsAny<string>()), Times.Never);
        _bookingRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
