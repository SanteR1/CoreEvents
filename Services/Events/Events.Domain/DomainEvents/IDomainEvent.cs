namespace Events.Domain.DomainEvents;

public interface IDomainEvent
{
    Guid EventId { get; }
}
