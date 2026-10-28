using System.Text.Json;
using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Infrastructure;
using CoreEvents.Shared.Contracts.Registry;
using CoreEvents.Shared.Contracts.Serialization;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Caching;
using Events.Application.Abstractions.Messaging;
using Events.Infrastructure.Data;
using Events.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Events.Tests.Data;

public sealed class OutboxServiceTests : IDisposable
{
    private readonly EventsDbContext _dbContext;
    private readonly Mock<IEventTopicMapper> _topicMapperMock = new();
    private readonly Mock<ICorrelationContext> _correlationContextMock = new();
    private readonly IntegrationEventRegistry _registry = new();
    private readonly OutboxService _outboxService;

    public OutboxServiceTests()
    {
        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseNpgsql("Host=localhost;Database=dummy")
            .Options;

        _dbContext = new EventsDbContext(options);

        _topicMapperMock.Setup(m => m.GetTopicFor<EventCreated>()).Returns(KafkaTopics.EventTopic);
        _topicMapperMock.Setup(m => m.GetTopicFor<EventBookingCancellationCompleted>()).Returns(KafkaTopics.EventConfirmed);

        _outboxService = new OutboxService(_dbContext, _topicMapperMock.Object, _registry, _correlationContextMock.Object);
    }

    [Fact]
    public void AddCacheInvalidation_WhenCalledMultipleTimesWithSameEventId_ShouldDeduplicateInChangeTracker()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act - вызываем дважды в рамках одного UoW
        _outboxService.AddCacheInvalidation(eventId);
        _outboxService.AddCacheInvalidation(eventId);

        // Assert
        var cacheMessages = _dbContext.ChangeTracker.Entries<OutboxMessage>()
            .Where(e => string.Equals(e.Entity.MessageType, OutboxService.CacheInvalidationMessageType, StringComparison.Ordinal))
            .ToList();

        cacheMessages.Should().HaveCount(1, "Повторные вызовы AddCacheInvalidation для одного eventId должны дедуплицироваться");
        cacheMessages[0].Entity.Key.Should().Be(CacheKeys.Event(eventId));
        cacheMessages[0].Entity.Topic.Should().Be(OutboxService.InternalCacheTopic);
    }

    [Fact]
    public void AddCacheInvalidation_WhenCalledWithDifferentEventIds_ShouldCreateSeparateMessages()
    {
        // Arrange
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();

        // Act
        _outboxService.AddCacheInvalidation(eventId1);
        _outboxService.AddCacheInvalidation(eventId2);

        // Assert
        var cacheMessages = _dbContext.ChangeTracker.Entries<OutboxMessage>()
            .Where(e => string.Equals(e.Entity.MessageType, OutboxService.CacheInvalidationMessageType, StringComparison.Ordinal))
            .ToList();

        cacheMessages.Should().HaveCount(2);
        cacheMessages.Select(m => m.Entity.Key).Should().Contain([CacheKeys.Event(eventId1), CacheKeys.Event(eventId2)]);
    }

    [Fact]
    public void AddIntegrationEvent_ShouldSetCausationAndCorrelationFromContext()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();

        _correlationContextMock.Setup(c => c.CorrelationId).Returns(correlationId);
        _correlationContextMock.Setup(c => c.CausationId).Returns(causationId);

        var eventId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var @event = new EventCreated
        {
            EventId = eventId,
            Title = "Conference",
            StartAt = now.UtcDateTime.AddDays(1),
            EndAt = now.UtcDateTime.AddDays(2),
            TotalSeats = 100,
            UnitPrice = 500m,
            Currency = "KZT",
            PriceVersion = 1,
            IsActive = true,
            Version = 1,
            CreatedAt = now
        };

        // Act
        _outboxService.AddIntegrationEvent(@event, partitionKey: eventId.ToString());

        // Assert
        var message = _dbContext.ChangeTracker.Entries<OutboxMessage>().Single().Entity;

        message.CorrelationId.Should().Be(correlationId);
        message.CausationId.Should().Be(causationId);
        message.Key.Should().Be(eventId.ToString());
        message.MessageType.Should().Be("event.created.v1");
        message.Topic.Should().Be(KafkaTopics.EventTopic);

        // Проверка payload
        var deserializedPayload = JsonSerializer.Deserialize<EventCreated>(message.Payload, IntegrationEventJsonOptions.Default);
        deserializedPayload.Should().NotBeNull();
        deserializedPayload.Title.Should().Be("Conference");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
