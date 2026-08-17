using Events.Domain.DomainEvents;
using MediatR;

namespace Events.Application.Abstractions.Messaging;

public class DomainEventNotification : INotification
{
    public IDomainEvent DomainEvent { get; }

    public DomainEventNotification(IDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}
