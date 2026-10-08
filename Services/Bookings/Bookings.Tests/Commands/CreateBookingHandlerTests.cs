using AwesomeAssertions;
using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Commands;
using Bookings.Application.Configuration;
using Bookings.Application.Exceptions;
using Bookings.Domain.Entities;
using Bookings.Domain.Exceptions;
using CoreEvents.Shared.Contracts.Events;
using Moq;

namespace Bookings.Tests.Commands;

public class CreateBookingHandlerTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IEventProjectionRepository> _eventProjectionRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly BookingSettings _bookingSettings;
    private readonly CreateBookingHandler _handler;

    public CreateBookingHandlerTests()
    {
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _eventProjectionRepositoryMock = new Mock<IEventProjectionRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();
        _bookingSettings = new BookingSettings { MaxBookingsPerUser = 10 };

        _handler = new CreateBookingHandler(
            _bookingRepositoryMock.Object,
            _eventProjectionRepositoryMock.Object,
            _outboxServiceMock.Object,
            _bookingSettings);
    }

    private static EventProjection CreateValidProjection(Guid eventId, int totalSeats = 100) =>
        EventProjection.Create(
            eventId,
            "Concert",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            500m,
            "KZT",
            1,
            totalSeats,
            isActive: true,
            version: 1,
            DateTimeOffset.UtcNow);

    [Fact]
    public async Task Handle_Should_CreateBookingAndPublishReservationRequested_When_PreCheckPasses()
    {
        // Arrange
        var command = new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid(), 2);
        var projection = CreateValidProjection(command.EventId);

        _bookingRepositoryMock
            .Setup(repo => repo.GetBookingCountForUserAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _eventProjectionRepositoryMock
            .Setup(r => r.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projection);

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
                It.Is<BookingReservationRequested>(e =>
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
    public async Task Handle_Should_DefaultToOneSeat_When_SeatsIsNull()
    {
        // Arrange
        var command = new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid(), Seats: null);
        var projection = CreateValidProjection(command.EventId);

        _bookingRepositoryMock
            .Setup(r => r.GetBookingCountForUserAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _eventProjectionRepositoryMock
            .Setup(r => r.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projection);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _outboxServiceMock.Verify(o => o.Publish(
                It.Is<BookingReservationRequested>(e => e.Seats == 1),
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
        _outboxServiceMock.Verify(o => o.Publish(It.IsAny<BookingReservationRequested>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowEventNotFoundException_When_EventProjectionNotFound()
    {
        // Arrange
        var command = new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid());

        _bookingRepositoryMock
            .Setup(r => r.GetBookingCountForUserAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _eventProjectionRepositoryMock
            .Setup(r => r.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventProjection?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EventNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowEventNotActiveException_When_EventIsNotActive()
    {
        // Arrange
        var command = new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid());
        var projection = EventProjection.Create(
            command.EventId, "Concert", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2),
            500m, "KZT", 1, 100, isActive: false, version: 1, DateTimeOffset.UtcNow);

        _bookingRepositoryMock
            .Setup(r => r.GetBookingCountForUserAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _eventProjectionRepositoryMock
            .Setup(r => r.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projection);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EventNotActiveException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowPastEventBookingException_When_EventIsInThePast()
    {
        // Arrange
        var command = new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid());
        var projection = EventProjection.Create(
            command.EventId, "Concert", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1),
            500m, "KZT", 1, 100, isActive: true, version: 1, DateTimeOffset.UtcNow);

        _bookingRepositoryMock
            .Setup(r => r.GetBookingCountForUserAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _eventProjectionRepositoryMock
            .Setup(r => r.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projection);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PastEventBookingException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowValidationException_When_RequestedSeatsExceedTotalSeats()
    {
        // Arrange
        var command = new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid(), 150);
        var projection = CreateValidProjection(command.EventId, totalSeats: 100);

        _bookingRepositoryMock
            .Setup(r => r.GetBookingCountForUserAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _eventProjectionRepositoryMock
            .Setup(r => r.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projection);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Bookings.Domain.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowValidationException_When_RequestedSeatsZeroOrNegative()
    {
        // Arrange
        var command = new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid(), 0);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Bookings.Domain.Exceptions.ValidationException>();
    }
}
