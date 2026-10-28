using CoreEvents.Shared.Contracts.Events;

namespace Events.Application.Abstractions;

public interface IOutboxService
{
    /// <summary>
    /// Добавление строго типизированного контракта Integration Event в ChangeTracker для публикации в Kafka.
    /// </summary>
    void AddIntegrationEvent<TEvent>(
        TEvent integrationEvent,
        string partitionKey)
        where TEvent : IIntegrationEvent;

    /// <summary>
    /// Добавление намерения инвалидации кэша события с дедупликацией в рамках текущего UoW.
    /// </summary>
    void AddCacheInvalidation(Guid eventId);
}
