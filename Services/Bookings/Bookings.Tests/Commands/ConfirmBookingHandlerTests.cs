using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Commands;
using Bookings.Domain.Entities;
using Bookings.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Bookings.Tests.Commands;

public class ConfirmBookingHandlerTests
{
    private readonly Mock<IBookingRepository> _repositoryMock;
    private readonly Mock<ILogger<ConfirmBookingHandler>> _loggerMock;
    private readonly ConfirmBookingHandler _handler;

    public ConfirmBookingHandlerTests()
    {
        _repositoryMock = new Mock<IBookingRepository>();
        _loggerMock = new Mock<ILogger<ConfirmBookingHandler>>();

        _handler = new ConfirmBookingHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_Should_LogWarningAndReturnUnit_When_BookingIsNotFound()
    {
        // Arrange
        var command = new ConfirmBookingCommand(Guid.NewGuid(), Guid.NewGuid());

        _repositoryMock
            .Setup(r => r.GetByIdAsync(command.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(command.BookingId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _repositoryMock.Verify(r => r.Update(It.IsAny<Booking>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ConfirmBookingAndUpdateRepository_When_BookingIsFound()
    {
        // Arrange
        var command = new ConfirmBookingCommand(Guid.NewGuid(), Guid.NewGuid());

        var booking = Booking.Create(command.EventId, Guid.NewGuid());

        _repositoryMock
            .Setup(r => r.GetByIdAsync(command.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        booking.Status.Should().Be(BookingStatus.Confirmed);

        _repositoryMock.Verify(r => r.Update(booking), Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _loggerMock.Verify(
            logger => logger.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
