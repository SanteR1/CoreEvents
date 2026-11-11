using Events.Domain.DomainEvents;

namespace Events.Application.Abstractions.Messaging;

public interface IDomainEventDispatcher
{
    Task DispatchPreCommitAsync(IDomainEvent domainEvent, CancellationToken ct);
    Task DispatchPostCommitAsync(IDomainEvent domainEvent, CancellationToken ct);
}
