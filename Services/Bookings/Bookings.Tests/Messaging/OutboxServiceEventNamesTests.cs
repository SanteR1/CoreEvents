using AwesomeAssertions;
using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Messaging;
using Bookings.Infrastructure.Data;
using Bookings.Infrastructure.Messaging.Kafka;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Registry;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Bookings.Tests.Messaging;

public sealed class OutboxServiceEventNamesTests : IDisposable
{
    private readonly BookingsDbContext _dbContext;
    private readonly Mock<IEventTopicMapper> _topicMapperMock;
    private readonly Mock<ICorrelationContext> _correlationContextMock;
    private readonly IntegrationEventRegistry _registry;
    private readonly OutboxService _outboxService;

    public OutboxServiceEventNamesTests()
    {
        var options = new DbContextOptionsBuilder<BookingsDbContext>()
            .UseNpgsql("Host=localhost;Database=dummy")
            .Options;
        _dbContext = new BookingsDbContext(options);

        _topicMapperMock = new Mock<IEventTopicMapper>();
        _topicMapperMock.Setup(m => m.GetTopicFor<BookingCancelled>()).Returns("test-topic");

        _correlationContextMock = new Mock<ICorrelationContext>();
        _correlationContextMock.Setup(c => c.CorrelationId).Returns(Guid.NewGuid());
        _correlationContextMock.Setup(c => c.CausationId).Returns(Guid.NewGuid());

        _registry = new IntegrationEventRegistry();
        _outboxService = new OutboxService(_dbContext, _topicMapperMock.Object, _correlationContextMock.Object, _registry);
    }

    [Fact]
    public void Publish_BookingCancelled_ShouldUseEventName()
    {
        // Arrange
        var @event = new BookingCancelled
        {
            BookingId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Reason = CancellationReason.UserCancelled,
            CancelledAt = DateTimeOffset.UtcNow
        };

        // Act
        _outboxService.Publish(@event, partitionKey: @event.EventId.ToString());

        // Assert
        var message = _dbContext.OutboxMessages.Local.FirstOrDefault();
        message.Should().NotBeNull();
        if (message != null)
        {
            message.MessageType.Should().Be(_registry.GetName<BookingCancelled>());
            message.MessageType.Should().Be("booking.cancelled.v1");
        }
    }

    private sealed record UnannotatedEvent : IIntegrationEvent;

    [Fact]
    public void Publish_UnannotatedEvent_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var @event = new UnannotatedEvent();

        // Act
        Action act = () => _outboxService.Publish(@event, "partition");

        // Assert
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage($"*{nameof(UnannotatedEvent)}*");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
