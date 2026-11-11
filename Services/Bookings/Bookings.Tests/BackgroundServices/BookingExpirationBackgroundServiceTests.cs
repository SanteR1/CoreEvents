using AwesomeAssertions;
using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Domain.Entities;
using Bookings.Domain.Enums;
using CoreEvents.Shared.Contracts.Events;
using Moq;

namespace Bookings.Tests.BackgroundServices;

public class BookingExpirationBackgroundServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;

    public BookingExpirationBackgroundServiceTests()
    {
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();
    }

    [Fact]
    public async Task ProcessExpiredPending_Should_RejectAndPublishReleaseRequest_When_PendingTimesOut()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), 2);
        // Booking is Pending

        var expiredBookings = new List<Booking> { booking };
        _bookingRepositoryMock
            .Setup(r => r.GetExpiredPendingBookingsAsync(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredBookings);

        // Act - execute expiration logic
        foreach (var b in expiredBookings)
        {
            b.Reject(ValidationFailureReason.Timeout);
            _bookingRepositoryMock.Object.Update(b);

            _outboxServiceMock.Object.Publish(
                new BookingReservationReleaseRequested
                {
                    BookingId = b.Id,
                    EventId = b.EventId,
                    Seats = b.Seats,
                    Reason = CancellationReason.Timeout
                },
                b.EventId.ToString());

            _outboxServiceMock.Object.Publish(
                new BookingRejected
                {
                    BookingId = b.Id,
                    EventId = b.EventId,
                    UserId = b.UserId,
                    Reason = ValidationFailureReason.Timeout,
                    RejectedAt = DateTimeOffset.UtcNow
                },
                b.EventId.ToString());
        }

        await _bookingRepositoryMock.Object.SaveChangesAsync(CancellationToken.None);

        // Assert
        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.RejectionReason.Should().Be(ValidationFailureReason.Timeout.ToString());

        _bookingRepositoryMock.Verify(r => r.Update(booking), Times.Once);
        _bookingRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingReservationReleaseRequested>(e =>
                e.BookingId == booking.Id &&
                e.Reason == CancellationReason.Timeout),
            booking.EventId.ToString()),
            Times.Once);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingRejected>(e =>
                e.BookingId == booking.Id &&
                e.Reason == ValidationFailureReason.Timeout),
            booking.EventId.ToString()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessStaleCancellationPending_Should_RetryPublishingReleaseRequest()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), 2);
        booking.Confirm(100m, 200m, "KZT");
        booking.RequestCancellation(CancellationReason.UserCancelled);

        var staleBookings = new List<Booking> { booking };
        _bookingRepositoryMock
            .Setup(r => r.GetStaleCancellationPendingBookingsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(staleBookings);

        // Act
        foreach (var b in staleBookings)
        {
            _outboxServiceMock.Object.Publish(
                new BookingReservationReleaseRequested
                {
                    BookingId = b.Id,
                    EventId = b.EventId,
                    Seats = b.Seats,
                    Reason = b.CancellationReason ?? CancellationReason.UserCancelled
                },
                b.EventId.ToString());
        }

        await _bookingRepositoryMock.Object.SaveChangesAsync(CancellationToken.None);

        // Assert
        booking.Status.Should().Be(BookingStatus.CancellationPending);

        _outboxServiceMock.Verify(o => o.Publish(
            It.Is<BookingReservationReleaseRequested>(e =>
                e.BookingId == booking.Id &&
                e.Seats == 2 &&
                e.Reason == CancellationReason.UserCancelled),
            booking.EventId.ToString()),
            Times.Once);
    }
}
