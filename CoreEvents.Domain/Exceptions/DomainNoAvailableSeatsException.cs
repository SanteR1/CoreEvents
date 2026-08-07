namespace CoreEvents.Domain.Exceptions;

public class DomainNoAvailableSeatsException(Guid eventId)
    : DomainException($"No available seats for event with ID = '{eventId}'.")
{
    public override string ErrorCode => "Event.NoAvailableSeats";
    public Guid EventId { get; } = eventId;
}
