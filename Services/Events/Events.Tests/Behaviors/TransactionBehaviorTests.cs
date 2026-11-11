using System.Data;
using AwesomeAssertions;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Persistence;
using Events.Application.Common.Behaviors;
using Events.Domain.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Events.Tests.Behaviors;

public class TransactionBehaviorTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<ILogger<TransactionBehavior<TestTransactionalCommand, string>>> _loggerMock = new();

    private TransactionBehavior<TestTransactionalCommand, string> CreateBehavior() =>
        new(_unitOfWorkMock.Object, _dispatcherMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_WhenRequestIsNotIBaseCommand_ShouldBypassTransaction()
    {
        // Arrange
        var nonCommandLoggerMock = new Mock<ILogger<TransactionBehavior<TestQuery, string>>>();
        var behavior = new TransactionBehavior<TestQuery, string>(
            _unitOfWorkMock.Object, _dispatcherMock.Object, nonCommandLoggerMock.Object);

        var query = new TestQuery();
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("query-result");
        };

        // Act
        var result = await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        result.Should().Be("query-result");
        nextCalled.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenHasActiveTransaction_ShouldThrowInvalidOperationException_FailFast()
    {
        // Arrange
        _unitOfWorkMock.Setup(u => u.HasActiveTransaction).Returns(true);
        var behavior = CreateBehavior();

        var command = new TestTransactionalCommand();
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        // Act
        var act = () => behavior.Handle(command, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Обнаружен вложенный вызов транзакционной команды*");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NormalFlow_ShouldExecuteInStrictTwoPhaseOrder()
    {
        // Arrange
        var behavior = CreateBehavior();
        var command = new TestTransactionalCommand();
        var testEvent = new TestDomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var executionOrder = new List<string>();

        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(IsolationLevel.RepeatableRead, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => executionOrder.Add("BeginTransaction"));

        RequestHandlerDelegate<string> next = _ =>
        {
            executionOrder.Add("HandlerNext");
            return Task.FromResult("handler-result");
        };

        var collectCalls = 0;
        _unitOfWorkMock.Setup(u => u.CollectDomainEvents())
            .Returns(() =>
            {
                collectCalls++;
                return collectCalls == 1 ? [testEvent] : [];
            });

        _dispatcherMock.Setup(d => d.DispatchPreCommitAsync(testEvent, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => executionOrder.Add("DispatchPreCommit"));

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => executionOrder.Add("SaveChanges"));

        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => executionOrder.Add("CommitTransaction"));

        _dispatcherMock.Setup(d => d.DispatchPostCommitAsync(testEvent, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => executionOrder.Add("DispatchPostCommit"));

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be("handler-result");

        executionOrder.Should().Equal(
            "BeginTransaction",
            "HandlerNext",
            "DispatchPreCommit",
            "SaveChanges",
            "CommitTransaction",
            "DispatchPostCommit");
    }

    [Fact]
    public async Task Handle_WhenEventsCascadeAcrossMultipleRounds_ShouldDispatchAllRounds()
    {
        // Arrange
        var behavior = CreateBehavior();
        var command = new TestTransactionalCommand();
        var event1 = new TestDomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var event2 = new TestDomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(1));

        var round = 0;
        _unitOfWorkMock.Setup(u => u.CollectDomainEvents())
            .Returns(() =>
            {
                round++;
                return round switch
                {
                    1 => [event1],
                    2 => [event2],
                    _ => []
                };
            });

        var preCommitDispatched = new List<IDomainEvent>();
        _dispatcherMock.Setup(d => d.DispatchPreCommitAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<IDomainEvent, CancellationToken>((e, _) => preCommitDispatched.Add(e));

        var postCommitDispatched = new List<IDomainEvent>();
        _dispatcherMock.Setup(d => d.DispatchPostCommitAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<IDomainEvent, CancellationToken>((e, _) => postCommitDispatched.Add(e));

        // Act
        await behavior.Handle(command, _ => Task.FromResult("ok"), CancellationToken.None);

        // Assert
        preCommitDispatched.Should().Equal(event1, event2);
        postCommitDispatched.Should().Equal(event1, event2);
    }

    [Fact]
    public async Task Handle_WhenEventsExceedMaxDispatchRounds_ShouldThrowInvalidOperationException_AndRollback()
    {
        // Arrange
        var behavior = CreateBehavior();
        var command = new TestTransactionalCommand();
        var endlessEvent = new TestDomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);

        // Бесконечный цикл событий
        _unitOfWorkMock.Setup(u => u.CollectDomainEvents()).Returns([endlessEvent]);

        // Act
        var act = () => behavior.Handle(command, _ => Task.FromResult("ok"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Превышен лимит раундов диспетчеризации доменных событий*");

        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_ShouldRollback_AndRethrowOriginalException()
    {
        // Arrange
        var behavior = CreateBehavior();
        var command = new TestTransactionalCommand();
        var originalException = new InvalidOperationException("Business failure in command handler");

        RequestHandlerDelegate<string> next = _ => throw originalException;

        // Act
        var act = () => behavior.Handle(command, next, CancellationToken.None);

        // Assert
        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Should().BeSameAs(originalException);

        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _dispatcherMock.Verify(d => d.DispatchPostCommitAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRollbackThrowsException_ShouldPreserveOriginalException_AndLogWarning()
    {
        // Arrange
        var behavior = CreateBehavior();
        var command = new TestTransactionalCommand();
        var originalException = new InvalidOperationException("Business error");
        var rollbackException = new InvalidOperationException("Database connection dropped during rollback");

        _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(rollbackException);

        RequestHandlerDelegate<string> next = _ => throw originalException;

        // Act
        var act = () => behavior.Handle(command, next, CancellationToken.None);

        // Assert
        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Should().BeSameAs(originalException, "Ошибка отката не должна маскировать оригинальное бизнес-исключение");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Сбой при откате транзакции")),
                rollbackException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPostCommitHandlerFails_ShouldNotThrow_NorRollback_IsBestEffort()
    {
        // Arrange
        var behavior = CreateBehavior();
        var command = new TestTransactionalCommand();
        var testEvent = new TestDomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var collectCalls = 0;
        _unitOfWorkMock.Setup(u => u.CollectDomainEvents())
            .Returns(() =>
            {
                collectCalls++;
                return collectCalls == 1 ? [testEvent] : [];
            });

        _dispatcherMock.Setup(d => d.DispatchPostCommitAsync(testEvent, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Telemetry service unavailable"));

        // Act
        var result = await behavior.Handle(command, _ => Task.FromResult("ok"), CancellationToken.None);

        // Assert
        result.Should().Be("ok");
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Post-commit обработчик")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public sealed record TestTransactionalCommand : ICommand<string>;
    public sealed record TestQuery : IRequest<string>;
    public sealed record TestDomainEvent(Guid Id, DateTimeOffset OccurredAt) : IDomainEvent;
}
