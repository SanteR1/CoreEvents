using CoreEvents.Shared.Contracts.Exceptions;

namespace Events.Domain.Exceptions
{
    public class NoAvailableSeatsException(Guid eventId)
        : ConflictException($"No available seats for event with ID = '{eventId}'.")
    {
        public override string ErrorCode => "Event.NoAvailableSeats";
        public override object ErrorData => new { eventId };
    }
}
