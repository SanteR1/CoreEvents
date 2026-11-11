using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Domain.DomainEvents;

namespace Events.Application.Events;

internal sealed class PublishEventCreatedHandler(
    IOutboxService outboxService) : IPreCommitHandler<EventCreatedDomainEvent>
{
    public Task Handle(EventCreatedDomainEvent e, CancellationToken ct)
    {
        outboxService.AddIntegrationEvent(
            new EventCreated
            {
                EventId = e.EventId,
                Title = e.Title,
                StartAt = e.StartAt,
                EndAt = e.EndAt,
                UnitPrice = e.UnitPrice,
                Currency = e.Currency,
                PriceVersion = e.PriceVersion,
                TotalSeats = e.TotalSeats,
                IsActive = e.IsActive,
                Version = e.Version,
                CreatedAt = e.OccurredAt
            },
            partitionKey: e.EventId.ToString());
        return Task.CompletedTask;
    }
}
