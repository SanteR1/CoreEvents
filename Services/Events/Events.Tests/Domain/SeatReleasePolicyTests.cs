using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using Events.Domain.Entities;
using Events.Domain.Enums;
using Events.Domain.Policies;
using Events.Tests.Infrastructure;
using Xunit;

namespace Events.Tests.Domain;

public class SeatReleasePolicyTests
{
    [Fact]
    public void Apply_WhenReservationIsAlreadyReleased_ShouldReturnAlreadyReleased()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var @event = TestEventFactory.Create(seats: 10);
        var bookingId = Guid.NewGuid();
        @event.TryReserveSeats(bookingId, 2, now);
        var reservation = SeatReservation.CreateReserved(
            bookingId, @event.Id, 2, 100m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());
        reservation.Release(); // Status = Released

        // Act
        var outcome = SeatReleasePolicy.Apply(reservation, @event, now);

        // Assert
        outcome.Should().Be(ReleaseOutcome.AlreadyReleased);
        outcome.IsIntegrityViolation().Should().BeFalse();
        @event.AvailableSeats.Should().Be(8, "Места в событии не должны увеличиваться при повторном освобождении");
    }

    [Fact]
    public void Apply_WhenReservationIsNotReserved_ShouldReturnNotReserved()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var @event = TestEventFactory.Create(seats: 10);
        var reservation = SeatReservation.CreateRejected(
            Guid.NewGuid(), @event.Id, 2, ValidationFailureReason.SeatsNotAvailable,
            Guid.NewGuid(), Guid.NewGuid());

        // Act
        var outcome = SeatReleasePolicy.Apply(reservation, @event, now);

        // Assert
        outcome.Should().Be(ReleaseOutcome.NotReserved);
        outcome.IsIntegrityViolation().Should().BeFalse();
    }

    [Fact]
    public void Apply_WhenReservationIsReserved_AndEventIsNull_ShouldReturnEventMissing()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var reservation = SeatReservation.CreateReserved(
            Guid.NewGuid(), Guid.NewGuid(), 2, 100m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var outcome = SeatReleasePolicy.Apply(reservation, null, now);

        // Assert
        outcome.Should().Be(ReleaseOutcome.EventMissing);
        outcome.IsIntegrityViolation().Should().BeTrue();
    }

    [Fact]
    public void Apply_WhenReservationIsReserved_AndReleaseSucceeds_ShouldReturnReleased()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var @event = TestEventFactory.Create(seats: 10);
        var bookingId = Guid.NewGuid();
        @event.TryReserveSeats(bookingId, 3, now);
        @event.AvailableSeats.Should().Be(7);

        var reservation = SeatReservation.CreateReserved(
            bookingId, @event.Id, 3, 100m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var outcome = SeatReleasePolicy.Apply(reservation, @event, now);

        // Assert
        outcome.Should().Be(ReleaseOutcome.Released);
        outcome.IsIntegrityViolation().Should().BeFalse();
        reservation.Status.Should().Be(SeatsReservationStatus.Released);
        @event.AvailableSeats.Should().Be(10);
    }

    [Fact]
    public void Apply_WhenEventReleaseSeatsFails_ShouldReturnInvariantViolated()
    {
        // Arrange: у события AvailableSeats = 10 (TotalSeats = 10), если вернуть 5, превысит TotalSeats
        var now = DateTimeOffset.UtcNow;
        var @event = TestEventFactory.Create(seats: 10);
        var reservation = SeatReservation.CreateReserved(
            Guid.NewGuid(), @event.Id, 5, 100m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var outcome = SeatReleasePolicy.Apply(reservation, @event, now);

        // Assert
        outcome.Should().Be(ReleaseOutcome.InvariantViolated);
        outcome.IsIntegrityViolation().Should().BeTrue();
    }
}
