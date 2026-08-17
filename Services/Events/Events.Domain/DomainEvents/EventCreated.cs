namespace Events.Domain.DomainEvents;

public sealed record EventCreated(Guid EventId) : IDomainEvent { }
