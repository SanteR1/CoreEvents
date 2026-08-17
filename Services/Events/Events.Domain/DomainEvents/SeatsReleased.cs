namespace Events.Domain.DomainEvents;

public sealed record SeatsReleased(Guid EventId) : ICacheInvalidationEvent;
