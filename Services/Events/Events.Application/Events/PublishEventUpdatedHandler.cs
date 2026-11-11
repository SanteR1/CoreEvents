using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Domain.DomainEvents;

namespace Events.Application.Events;

internal sealed class PublishEventUpdatedHandler(IOutboxService outboxService) : IPreCommitHandler<EventUpdatedDomainEvent>
{
    public Task Handle(EventUpdatedDomainEvent e, CancellationToken ct)
    {
        outboxService.AddIntegrationEvent(
    new EventUpdated
    {
        EventId = e.EventId,
        Title = e.Title,
        Description = e.Description,
        StartAt = e.StartAt,
        EndAt = e.EndAt,
        UnitPrice = e.UnitPrice,
        Currency = e.Currency,
        PriceVersion = e.PriceVersion,
        TotalSeats = e.TotalSeats,
        IsActive = e.IsActive,
        Version = e.Version,
        UpdatedAt = e.OccurredAt
    },
    partitionKey: e.EventId.ToString());
        return Task.CompletedTask;
    }
}
