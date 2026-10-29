namespace Events.Domain.DomainEvents;

public sealed record SeatsReserved(
    Guid EventId,
    Guid BookingId,
    int Seats,
    decimal UnitPrice,
    string Currency,
    DateTimeOffset OccurredAt) : IDomainEvent;
