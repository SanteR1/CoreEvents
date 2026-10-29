namespace Events.Domain.DomainEvents;

public sealed record EventCancelled(
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent;
