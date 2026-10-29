namespace Events.Domain.DomainEvents;

public sealed record SeatsReleased(
    Guid EventId,
    Guid BookingId,
    int Seats,
    DateTimeOffset OccurredAt) : IDomainEvent;
