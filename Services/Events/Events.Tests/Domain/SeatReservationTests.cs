using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using Events.Domain.Entities;
using Events.Domain.Enums;
using Events.Domain.Exceptions;

namespace Events.Tests.Domain;

public class SeatReservationTests
{
    [Fact]
    public void CreateReserved_WithValidParameters_ShouldCalculateTotalPriceCorrectly()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var correlationId = bookingId;
        var causationId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        // Act
        var reservation = SeatReservation.CreateReserved(
            bookingId,
            eventId,
            seats: 3,
            unitPrice: 100m,
            currency: "KZT",
            discountAmount: 20m,
            priceVersion: 1L,
            expiresAt: expiresAt,
            correlationId: correlationId,
            causationId: causationId);

        // Assert
        reservation.Status.Should().Be(SeatReservationStatus.Reserved);
        reservation.BookingId.Should().Be(bookingId);
        reservation.EventId.Should().Be(eventId);
        reservation.Seats.Should().Be(3);
        reservation.UnitPrice.Should().Be(100m);
        reservation.DiscountAmount.Should().Be(20m);
        reservation.TotalPrice.Should().Be(280m); // 3 * 100 - 20 = 280
        reservation.Currency.Should().Be("KZT");
        reservation.PriceVersion.Should().Be(1L);
        reservation.ExpiresAt.Should().Be(expiresAt);
        reservation.RejectionReason.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateReserved_WithNonPositiveSeats_ShouldThrowArgumentOutOfRangeException(int seats)
    {
        // Act
        Action act = () => SeatReservation.CreateReserved(
            Guid.NewGuid(), Guid.NewGuid(), seats, 100m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateReserved_WithNegativeUnitPrice_ShouldThrowArgumentOutOfRangeException()
    {
        // Act
        Action act = () => SeatReservation.CreateReserved(
            Guid.NewGuid(), Guid.NewGuid(), 2, -50m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateReserved_WithNegativeDiscount_ShouldThrowArgumentOutOfRangeException()
    {
        // Act
        Action act = () => SeatReservation.CreateReserved(
            Guid.NewGuid(), Guid.NewGuid(), 2, 100m, "KZT", -10m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateReserved_WithDiscountExceedingTotalPrice_ShouldThrowArgumentOutOfRangeException()
    {
        // Act (Seats=1, UnitPrice=50, Discount=60 => TotalPrice would be -10)
        Action act = () => SeatReservation.CreateReserved(
            Guid.NewGuid(), Guid.NewGuid(), 1, 50m, "KZT", 60m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateReserved_WithDefaultExpirationDate_ShouldThrowValidationException()
    {
        // Act
        Action act = () => SeatReservation.CreateReserved(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100m, "KZT", 0m, 1L,
            default, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Release_WhenReserved_ShouldTransitionToReleased()
    {
        // Arrange
        var reservation = SeatReservation.CreateReserved(
            Guid.NewGuid(), Guid.NewGuid(), 2, 100m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());

        // Act
        reservation.Release();

        // Assert
        reservation.Status.Should().Be(SeatReservationStatus.Released);
        reservation.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Release_WhenAlreadyReleased_ShouldReturnFalse()
    {
        // Arrange
        var reservation = SeatReservation.CreateReserved(
            Guid.NewGuid(), Guid.NewGuid(), 2, 100m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());
        reservation.Release();

        // Act
        var result = reservation.Release();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CreateCancelledBeforeReservation_ShouldSetStatusCorrectly()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var correlationId = bookingId;
        var causationId = Guid.NewGuid();

        // Act
        var reservation = SeatReservation.CreateCancelledBeforeReservation(bookingId, eventId, 2, correlationId, causationId);

        // Assert
        reservation.Status.Should().Be(SeatReservationStatus.CancelledBeforeReservation);
        reservation.BookingId.Should().Be(bookingId);
        reservation.EventId.Should().Be(eventId);
        reservation.Seats.Should().Be(2);
    }

    [Fact]
    public void CreateRejected_ShouldSetStatusAndReasonCorrectly()
    {
        // Act
        var reservation = SeatReservation.CreateRejected(
            Guid.NewGuid(), Guid.NewGuid(), 1, ValidationFailureReason.SeatsNotAvailable, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        reservation.Status.Should().Be(SeatReservationStatus.Rejected);
        reservation.RejectionReason.Should().Be(ValidationFailureReason.SeatsNotAvailable);
    }
}
