namespace Events.Domain.DomainEvents;

public sealed record EventCreated(
    Guid EventId,
    string Title,
    int TotalSeats,
    DateTimeOffset OccurredAt) : IDomainEvent;
