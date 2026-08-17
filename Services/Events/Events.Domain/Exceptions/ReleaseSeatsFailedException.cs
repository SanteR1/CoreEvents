using CoreEvents.Shared.Contracts.Exceptions;

namespace Events.Domain.Exceptions;

public class ReleaseSeatsFailedException(Guid eventId, Guid bookingId)
    : ConflictException($"Event with ID = '{eventId}' failed to release seats with Booking ID = '{bookingId}'.")
{
    public override string ErrorCode => "Event.ReleaseEventBooking";
    public override object ErrorData => new { eventId, bookingId };
}
