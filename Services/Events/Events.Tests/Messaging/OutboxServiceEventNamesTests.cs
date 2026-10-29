using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Registry;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Events.Tests.Messaging;

public sealed class OutboxServiceEventNamesTests : IDisposable
{
    private readonly EventsDbContext _dbContext;
    private readonly Mock<IEventTopicMapper> _topicMapperMock;
    private readonly Mock<ICorrelationContext> _correlationContextMock;
    private readonly IntegrationEventRegistry _registry;
    private readonly OutboxService _outboxService;

    public OutboxServiceEventNamesTests()
    {
        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseNpgsql("Host=localhost;Database=dummy")
            .Options;
        _dbContext = new EventsDbContext(options);

        _topicMapperMock = new Mock<IEventTopicMapper>();
        _topicMapperMock.Setup(m => m.GetTopicFor<EventBookingCancellationCompleted>()).Returns("test-topic");

        _correlationContextMock = new Mock<ICorrelationContext>();
        _correlationContextMock.Setup(c => c.CorrelationId).Returns(Guid.NewGuid());
        _correlationContextMock.Setup(c => c.CausationId).Returns(Guid.NewGuid());

        _registry = new IntegrationEventRegistry();
        _outboxService = new OutboxService(_dbContext, _topicMapperMock.Object, _registry, _correlationContextMock.Object);
    }

    [Fact]
    public void AddIntegrationEvent_EventBookingCancellationCompleted_ShouldUseVersionedWireName()
    {
        // Arrange
        var @event = new EventBookingCancellationCompleted
        {
            BookingId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            SeatsReleased = true
        };

        // Act
        _outboxService.AddIntegrationEvent(@event, partitionKey: @event.EventId.ToString());

        // Assert
        var message = _dbContext.OutboxMessages.Local.FirstOrDefault();
        message.Should().NotBeNull();
        if (message != null)
        {
            var expectedWireName = _registry.GetName<EventBookingCancellationCompleted>();
            message.MessageType.Should().Be(expectedWireName);
            message.MessageType.Should().Be("event.booking-cancellation-completed.v1");
            message.Topic.Should().Be("test-topic");
        }
    }

    private sealed record UnregisteredEvent : IIntegrationEvent;

    [Fact]
    public void AddIntegrationEvent_UnregisteredEvent_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var @event = new UnregisteredEvent();

        // Act
        Action act = () => _outboxService.AddIntegrationEvent(@event, "partition");

        // Assert
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage($"*{nameof(UnregisteredEvent)}*");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
