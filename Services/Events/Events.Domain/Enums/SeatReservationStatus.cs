namespace Events.Domain.Enums;

public enum SeatReservationStatus
{
    Reserved = 0,
    Released = 1,
    Rejected = 2,
    CancelledBeforeReservation = 3
}
