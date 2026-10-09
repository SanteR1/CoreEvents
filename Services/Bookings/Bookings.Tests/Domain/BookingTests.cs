using AwesomeAssertions;
using Bookings.Domain.Entities;
using Bookings.Domain.Enums;
using Bookings.Domain.Exceptions;
using CoreEvents.Shared.Contracts.Events;

namespace Bookings.Tests.Domain;

public class BookingTests
{
    [Fact]
    public void Confirm_ShouldChangeStatusToConfirmedAndSetPricingSnapshot()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), 2);

        // Act
        booking.Confirm(unitPrice: 100m, totalPrice: 180m, currency: "KZT", discountAmount: 20m);

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.UnitPrice.Should().Be(100m);
        booking.TotalPrice.Should().Be(180m);
        booking.Currency.Should().Be("KZT");
        booking.DiscountAmount.Should().Be(20m);
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void Confirm_WithNegativePricing_ShouldThrowValidationException()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        Action act = () => booking.Confirm(unitPrice: -10m, totalPrice: 100m, currency: "KZT");
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Reject_ShouldChangeStatusToRejectedAndSetRejectionReason()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());

        // Act
        booking.Reject(ValidationFailureReason.SeatsNotAvailable);

        // Assert
        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.RejectionReason.Should().Be(ValidationFailureReason.SeatsNotAvailable.ToString());
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void RequestCancellation_FromConfirmed_ShouldChangeStatusToCancellationPending()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());
        booking.Confirm(100m, 100m, "KZT");

        // Act
        booking.RequestCancellation(CancellationReason.UserCancelled);

        // Assert
        booking.Status.Should().Be(BookingStatus.CancellationPending);
        booking.CancellationReason.Should().Be(CancellationReason.UserCancelled);
        booking.CancellationRequestedAt.Should().NotBeNull();
    }

    [Fact]
    public void RequestCancellation_FromPending_ShouldThrowInvalidStatusTransitionException()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid()); // Pending

        // Act
        Action act = () => booking.RequestCancellation(CancellationReason.UserCancelled);

        // Assert
        act.Should().Throw<InvalidStatusTransitionException>();
    }

    [Fact]
    public void ApplyCancellation_FromCancellationPending_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());
        booking.Confirm(100m, 100m, "KZT");
        booking.RequestCancellation(CancellationReason.UserCancelled);

        // Act
        booking.ApplyCancellation();

        // Assert
        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void ApplyCancellation_FromConfirmed_ShouldChangeStatusToCancelled_ForBatchEventCancellation()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());
        booking.Confirm(100m, 100m, "KZT");

        // Act
        booking.ApplyCancellation(CancellationReason.EventCancelled);

        // Assert
        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancellationReason.Should().Be(CancellationReason.EventCancelled);
    }

    [Fact]
    public void Confirm_WhenAlreadyCancelled_ShouldThrowInvalidStatusTransitionException()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());
        booking.Confirm(100m, 100m, "KZT");
        booking.Cancel();

        // Act
        Action act = () => booking.Confirm(100m, 100m, "KZT");

        // Assert
        act.Should().Throw<InvalidStatusTransitionException>();
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

    [Fact]
    public void Create_ShouldSetDefaultCurrencyToKZT()
    {
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), 1);

        booking.Currency.Should().Be("KZT");
    }
}
