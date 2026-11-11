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

public class ApplyBookingCancellationHandlerTests
{
    private readonly Mock<IBookingRepository> _repositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<ILogger<ApplyBookingCancellationCommandHandler>> _loggerMock;
    private readonly ApplyBookingCancellationCommandHandler _handler;

    public ApplyBookingCancellationHandlerTests()
    {
        _repositoryMock = new Mock<IBookingRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();
        _loggerMock = new Mock<ILogger<ApplyBookingCancellationCommandHandler>>();

        _handler = new ApplyBookingCancellationCommandHandler(_repositoryMock.Object, _outboxServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_Should_LogWarningAndReturnUnit_When_BookingIsNotFound()
    {
        // Arrange
        var command = new ApplyBookingCancellationCommand(Guid.NewGuid());

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
        _outboxServiceMock.Verify(o => o.Publish(It.IsAny<BookingCancelled>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_CancelBookingAndPublishToOutbox_When_BookingIsFound()
    {
        // Arrange
        var command = new ApplyBookingCancellationCommand(Guid.NewGuid());

        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());

        _repositoryMock
            .Setup(r => r.GetByIdAsync(command.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        booking.Status.Should().Be(BookingStatus.Cancelled);

        _repositoryMock.Verify(r => r.Update(booking), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingCancelled>(e =>
                e.BookingId == booking.Id &&
                e.EventId == booking.EventId),
            booking.EventId.ToString()),
            Times.Once);
    }
}
