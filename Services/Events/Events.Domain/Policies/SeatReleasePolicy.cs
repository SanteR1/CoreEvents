namespace Events.Domain.Policies;

using Events.Domain.Entities;
using Events.Domain.Enums;

public static class SeatReleasePolicy
{
    public static ReleaseOutcome Apply(SeatReservation reservation, Event? @event, DateTimeOffset now)
    {
        switch (reservation.Status)
        {
            case SeatsReservationStatus.Released: return ReleaseOutcome.AlreadyReleased;
            case SeatsReservationStatus.Reserved: break;
            default: return ReleaseOutcome.NotReserved;
        }

        if (@event is null) return ReleaseOutcome.EventMissing;

        if (!@event.ReleaseSeats(reservation.BookingId, reservation.Seats, now))
            return ReleaseOutcome.InvariantViolated;

        if (!reservation.Release())
            return ReleaseOutcome.InvariantViolated;

        return ReleaseOutcome.Released;
    }

    public static bool IsIntegrityViolation(this ReleaseOutcome o) =>
        o is ReleaseOutcome.EventMissing or ReleaseOutcome.InvariantViolated;
}
