namespace Events.Domain.DomainEvents;

public sealed record SeatsReleasedDomainEvent(
    Guid EventId,
    Guid BookingId,
    int Seats,
    DateTimeOffset OccurredAt) : IDomainEvent;
