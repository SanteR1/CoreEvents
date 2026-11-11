using Events.Domain.Enums;

namespace Events.Domain.DomainEvents;

public sealed record EventCancelledDomainEvent(
    Guid EventId,
    long Version,
    DateTimeOffset OccurredAt) : IDomainEvent;
