namespace Events.Domain.DomainEvents;

public sealed record SeatsReserved(Guid EventId) : ICacheInvalidationEvent;
