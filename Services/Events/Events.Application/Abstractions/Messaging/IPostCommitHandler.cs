using System.Diagnostics.CodeAnalysis;
using Events.Domain.DomainEvents;

namespace Events.Application.Abstractions.Messaging;

[SuppressMessage("Major Code Smell", "S3246:Generic type parameters should be co/contravariant when possible", Justification = "Microsoft.Extensions.DependencyInjection is invariant")]
public interface IPostCommitHandler<TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken ct);
}
