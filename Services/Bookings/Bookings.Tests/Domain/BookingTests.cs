using Bookings.Domain.Entities;
using Bookings.Domain.Enums;
using Bookings.Domain.Exceptions;
using FluentAssertions;

namespace Bookings.Tests.Domain;

public class BookingTests
{
    [Fact]
    public void Confirm_ShouldChangeStatusToConfirmed()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        booking.ProcessedAt.Should().BeNull();
        booking.Confirm();
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reject_ShouldChangeStatusToRejected()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        booking.ProcessedAt.Should().BeNull();
        booking.Reject();
        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        booking.ProcessedAt.Should().BeNull();
        booking.Cancel();
        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithEmptyEventId_ShouldThrowsValidationException()
    {
        var eventId = Guid.Empty;
        var userId = Guid.NewGuid();

        Action act = () => Booking.Create(eventId, userId);
        var exceptionAssertion = act.Should().Throw<ValidationException>();

        exceptionAssertion.Which.ErrorCode.Should().Be("Booking.ValidationFailed");
        exceptionAssertion.Which.ValidationErrors.Should().ContainKey("eventId");
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrowsValidationException()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.Empty;

        Action act = () => Booking.Create(eventId, userId);
        var exceptionAssertion = act.Should().Throw<ValidationException>();

        exceptionAssertion.Which.ErrorCode.Should().Be("Booking.ValidationFailed");
        exceptionAssertion.Which.ValidationErrors.Should().ContainKey("userId");
    }

    [Fact]
    public void Create_WithZeroSeats_ShouldThrowsValidationException()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Action act = () => Booking.Create(eventId, userId, 0);
        var exceptionAssertion = act.Should().Throw<ValidationException>();

        exceptionAssertion.Which.ErrorCode.Should().Be("Booking.ValidationFailed");
        exceptionAssertion.Which.ValidationErrors.Should().ContainKey("seats");
    }

    [Fact]
    public void IsOwnedBy_WithOwnedUserId_ShouldReturnTrue()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var booking = Booking.Create(eventId, userId);
        var isOwned = booking.IsOwnedBy(userId);

        isOwned.Should().BeTrue();
    }

    [Fact]
    public void IsOwnedBy_WithOtherUserId_ShouldReturnFalse()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var booking = Booking.Create(eventId, userId);
        var isOwned = booking.IsOwnedBy(otherUserId);

        isOwned.Should().BeFalse();
    }

    [Fact]
    public void EnsureAccess_WithOwnerUserId_ShouldPass()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var booking = Booking.Create(eventId, userId);

        Action act = () => booking.EnsureAccess(userId);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureAccess_WithOtherUserId_ShouldThrowsNotBookingOwnerException()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var booking = Booking.Create(eventId, userId);

        Action act = () => booking.EnsureAccess(otherUserId);
        var exceptionAssertion = act.Should().Throw<NotBookingOwnerException>();

        exceptionAssertion.Which.ErrorCode.Should().Be("Booking.Denied");
        exceptionAssertion.Which.ErrorData.Should().BeEquivalentTo(new { bookingId = booking.Id });
    }
}
