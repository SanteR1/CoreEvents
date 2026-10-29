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

public class EventLifecycleHandlersTests
{
    private readonly Mock<IEventProjectionRepository> _eventProjectionRepositoryMock;
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<ILogger<EventLifecycleHandlers>> _loggerMock;
    private readonly EventLifecycleHandlers _handler;

    public EventLifecycleHandlersTests()
    {
        _eventProjectionRepositoryMock = new Mock<IEventProjectionRepository>();
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();
        _loggerMock = new Mock<ILogger<EventLifecycleHandlers>>();

        _handler = new EventLifecycleHandlers(
            _eventProjectionRepositoryMock.Object,
            _bookingRepositoryMock.Object,
            _outboxServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_SyncEventCreated_ShouldCreateProjection()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var command = new SyncEventCreatedCommand(
            eventId, "New Concert", now, now.AddDays(1), 500m, "KZT", 1L, 100, true, 1L, DateTimeOffset.UtcNow);

        _eventProjectionRepositoryMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventProjection?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _eventProjectionRepositoryMock.Verify(r => r.AddAsync(
            It.Is<EventProjection>(p =>
                p.Id == eventId &&
                p.Title == "New Concert" &&
                p.TotalSeats == 100),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _eventProjectionRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SyncEventUpdated_ShouldUpdateExistingProjection()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var projection = EventProjection.Create(
            eventId, "Old Title", now, now.AddDays(1), 500m, "KZT", 1L, 100, true, 1L, DateTimeOffset.UtcNow);

        var command = new SyncEventUpdatedCommand(
            eventId, "Updated Title", now, now.AddDays(1), 600m, "KZT", 2L, 120, true, 2L, DateTimeOffset.UtcNow);

        _eventProjectionRepositoryMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projection);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        projection.Title.Should().Be("Updated Title");
        projection.UnitPrice.Should().Be(600m);
        projection.TotalSeats.Should().Be(120);
        projection.Version.Should().Be(2L);

        _eventProjectionRepositoryMock.Verify(r => r.Update(projection), Times.Once);
        _eventProjectionRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SyncEventCancelled_ShouldCancelProjectionAndBatchCancelActiveBookings()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var projection = EventProjection.Create(
            eventId, "Concert", now, now.AddDays(1), 500m, "KZT", 1L, 100, true, 1L, DateTimeOffset.UtcNow);

        var booking1 = Booking.Create(eventId, Guid.NewGuid(), 2);
        booking1.Confirm(500m, 1000m, "KZT");

        var booking2 = Booking.Create(eventId, Guid.NewGuid(), 1);
        booking2.Confirm(500m, 500m, "KZT");
        booking2.RequestCancellation(CancellationReason.UserCancelled);

        _eventProjectionRepositoryMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projection);

        // First call returns 2 active bookings, second call returns empty list to stop loop
        _bookingRepositoryMock
            .SetupSequence(r => r.GetActiveBookingsByEventIdAsync(eventId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { booking1, booking2 })
            .ReturnsAsync(new List<Booking>());

        var command = new SyncEventCancelledCommand(eventId, "Artist ill", 2L, DateTimeOffset.UtcNow);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        projection.IsActive.Should().BeFalse();

        booking1.Status.Should().Be(BookingStatus.Cancelled);
        booking1.CancellationReason.Should().Be(CancellationReason.EventCancelled);

        booking2.Status.Should().Be(BookingStatus.Cancelled);
        booking2.CancellationReason.Should().Be(CancellationReason.EventCancelled);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingCancelled>(e => e.EventId == eventId && e.Reason == CancellationReason.EventCancelled),
            eventId.ToString()),
            Times.Exactly(2));

        _bookingRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
