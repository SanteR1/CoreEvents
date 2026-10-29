namespace Events.Domain.DomainEvents;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
