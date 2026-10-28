using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using Events.Application.Abstractions.Messaging;
using Events.Application.Common.Dispatcher;
using Events.Domain.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Events.Tests.Dispatcher;

[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP013:Await in using", Justification = "Test assertions await within using scope")]
public class DomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchPreCommitAsync_ShouldInvokeAllRegisteredPreCommitHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler1Mock = new Mock<IPreCommitHandler<TestEvent>>();
        var handler2Mock = new Mock<IPreCommitHandler<TestEvent>>();

        services.AddSingleton(handler1Mock.Object);
        services.AddSingleton(handler2Mock.Object);
        await using var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(
            serviceProvider,
            NullLogger<DomainEventDispatcher>.Instance);

        var domainEvent = new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);

        // Act
        await dispatcher.DispatchPreCommitAsync(domainEvent, CancellationToken.None);

        // Assert
        handler1Mock.Verify(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()), Times.Once);
        handler2Mock.Verify(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchPostCommitAsync_ShouldInvokeAllRegisteredPostCommitHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var postHandlerMock = new Mock<IPostCommitHandler<TestEvent>>();

        services.AddSingleton(postHandlerMock.Object);
        await using var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(
            serviceProvider,
            NullLogger<DomainEventDispatcher>.Instance);

        var domainEvent = new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);

        // Act
        await dispatcher.DispatchPostCommitAsync(domainEvent, CancellationToken.None);

        // Assert
        postHandlerMock.Verify(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_WhenNoHandlersRegistered_ShouldCompleteSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        await using var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(
            serviceProvider,
            NullLogger<DomainEventDispatcher>.Instance);

        var domainEvent = new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);

        // Act & Assert
        var actPre = () => dispatcher.DispatchPreCommitAsync(domainEvent, CancellationToken.None);
        var actPost = () => dispatcher.DispatchPostCommitAsync(domainEvent, CancellationToken.None);

        await actPre.Should().NotThrowAsync();
        await actPost.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Dispatch_ThrowsArgumentNullException_WhenEventIsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        await using var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(
            serviceProvider,
            NullLogger<DomainEventDispatcher>.Instance);

        // Act & Assert
        var actPre = () => dispatcher.DispatchPreCommitAsync(null!, CancellationToken.None);
        var actPost = () => dispatcher.DispatchPostCommitAsync(null!, CancellationToken.None);

        await actPre.Should().ThrowAsync<ArgumentNullException>();
        await actPost.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Dispatch_ThrowsOperationCanceledException_WhenCancellationTokenIsCancelled()
    {
        // Arrange
        var services = new ServiceCollection();
        var handlerMock = new Mock<IPreCommitHandler<TestEvent>>();
        services.AddSingleton(handlerMock.Object);
        await using var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(
            serviceProvider,
            NullLogger<DomainEventDispatcher>.Instance);

        var domainEvent = new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => dispatcher.DispatchPreCommitAsync(domainEvent, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        handlerMock.Verify(h => h.Handle(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Dispatch_ShouldReuseCompiledDelegates_OnSubsequentCalls()
    {
        // Arrange
        var services = new ServiceCollection();
        var handlerMock = new Mock<IPreCommitHandler<TestEvent>>();
        services.AddSingleton(handlerMock.Object);
        await using var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(
            serviceProvider,
            NullLogger<DomainEventDispatcher>.Instance);

        var event1 = new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var event2 = new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(1));

        // Act
        await dispatcher.DispatchPreCommitAsync(event1, CancellationToken.None);
        await dispatcher.DispatchPreCommitAsync(event2, CancellationToken.None);

        // Assert
        handlerMock.Verify(h => h.Handle(event1, It.IsAny<CancellationToken>()), Times.Once);
        handlerMock.Verify(h => h.Handle(event2, It.IsAny<CancellationToken>()), Times.Once);
    }

    public sealed record TestEvent(Guid Id, DateTimeOffset OccurredAt) : IDomainEvent;
}
