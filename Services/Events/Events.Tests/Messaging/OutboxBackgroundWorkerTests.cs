using System.Reflection;
using AwesomeAssertions;
using Events.Application.Abstractions.Caching;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Persistence;
using Events.Infrastructure.Data;
using Events.Infrastructure.Data.Entities;
using Events.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Events.Tests.Messaging;

public sealed class OutboxBackgroundWorkerTests : IDisposable
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IMessageProducer> _messageProducerMock = new();
    private readonly Mock<IExceptionAnalyzer> _exceptionAnalyzerMock = new();
    private readonly OutboxBackgroundWorker _worker;

    public OutboxBackgroundWorkerTests()
    {
        _worker = new OutboxBackgroundWorker(
            _scopeFactoryMock.Object,
            NullLogger<OutboxBackgroundWorker>.Instance,
            _messageProducerMock.Object,
            _exceptionAnalyzerMock.Object);
    }

    private async Task<bool> InvokeTryProcessMessageAsync(IServiceScope scope, OutboxMessage message, CancellationToken ct)
    {
        var method = typeof(OutboxBackgroundWorker).GetMethod(
            "TryProcessMessageAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingMethodException(nameof(OutboxBackgroundWorker), "TryProcessMessageAsync");

        var task = (Task<bool>)method.Invoke(_worker, [scope, message, ct])!;
        return await task;
    }

    [Fact]
    public async Task TryProcessMessageAsync_WhenCacheInvalidationMessage_ShouldCallCacheServiceAndMarkPublished()
    {
        // Arrange
        var cacheServiceMock = new Mock<ICacheService>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(ICacheService))).Returns(cacheServiceMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Key = "event:123",
            MessageType = OutboxService.CacheInvalidationMessageType,
            Topic = OutboxService.InternalCacheTopic,
            Payload = "{}",
            Headers = "{}",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var result = await InvokeTryProcessMessageAsync(scopeMock.Object, message, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        message.PublishedAt.Should().NotBeNull();
        cacheServiceMock.Verify(c => c.DeleteAsync("event:123", It.IsAny<CancellationToken>()), Times.Once);
        _messageProducerMock.Verify(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TryProcessMessageAsync_WhenIntegrationEvent_ShouldCallProducerAndMarkPublished()
    {
        // Arrange
        var scopeMock = new Mock<IServiceScope>();

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Key = "event:456",
            MessageType = "event.created.v1",
            Topic = "events-topic",
            Payload = "{\"Title\":\"Tech Conference\"}",
            Headers = "{\"traceparent\":\"00-abcdef\"}",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var result = await InvokeTryProcessMessageAsync(scopeMock.Object, message, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        message.PublishedAt.Should().NotBeNull();
        _messageProducerMock.Verify(p => p.ProduceAsync(
            "events-topic",
            "event:456",
            "{\"Title\":\"Tech Conference\"}",
            It.Is<Dictionary<string, string>>(h => h != null && h.ContainsKey("traceparent")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryProcessMessageAsync_WhenTransientFailure_ShouldSetNextRetryAndReturnFalse()
    {
        // Arrange
        var scopeMock = new Mock<IServiceScope>();
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Key = "key",
            MessageType = "event.created.v1",
            Topic = "events-topic",
            Payload = "{}",
            Headers = "{}",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var transientEx = new InvalidOperationException("Kafka broker temporary timeout");
        _messageProducerMock.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(transientEx);

        _exceptionAnalyzerMock.Setup(a => a.IsTransient(transientEx)).Returns(true);

        // Act
        var result = await InvokeTryProcessMessageAsync(scopeMock.Object, message, CancellationToken.None);

        // Assert
        result.Should().BeFalse("При временной ошибке цикл пачки должен прерваться");
        message.PublishedAt.Should().BeNull();
        message.NextRetryAt.Should().NotBeNull();
        message.IsDeadLettered.Should().BeFalse();
    }

    [Fact]
    public async Task TryProcessMessageAsync_WhenMaxRetryExceeded_ShouldMarkDeadLettered()
    {
        // Arrange
        var scopeMock = new Mock<IServiceScope>();
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Key = "key",
            MessageType = "event.created.v1",
            Topic = "events-topic",
            Payload = "{}",
            Headers = "{}",
            RetryCount = 4, // 5th attempt will reach maxRetryCount (5)
            CreatedAt = DateTimeOffset.UtcNow
        };

        var poisonEx = new InvalidOperationException("Poison message deserialization crash");
        _messageProducerMock.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(poisonEx);

        _exceptionAnalyzerMock.Setup(a => a.IsTransient(poisonEx)).Returns(false);

        // Act
        var result = await InvokeTryProcessMessageAsync(scopeMock.Object, message, CancellationToken.None);

        // Assert
        result.Should().BeTrue("Сообщение обработано (отправлено в DeadLetter), обработка очереди продолжается");
        message.RetryCount.Should().Be(5);
        message.IsDeadLettered.Should().BeTrue();
        message.PublishedAt.Should().BeNull();
        message.LastError.Should().Contain("Poison message");
    }

    public void Dispose()
    {
        _worker.Dispose();
    }
}
