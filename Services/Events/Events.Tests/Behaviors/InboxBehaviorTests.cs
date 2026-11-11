using AwesomeAssertions;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Persistence;
using Events.Application.Common.Behaviors;
using MediatR;
using Moq;
using Xunit;

namespace Events.Tests.Behaviors;

public class InboxBehaviorTests
{
    private readonly Mock<IInboxRepository> _inboxRepositoryMock = new();

    private InboxBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>() where TRequest : notnull =>
        new(_inboxRepositoryMock.Object);

    [Fact]
    public async Task Handle_WhenRequestIsNotIInboxCommand_ShouldBypassInboxRegistration()
    {
        // Arrange
        var behavior = CreateBehavior<TestRegularCommand, string>();
        var command = new TestRegularCommand();

        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("result");
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be("result");
        nextCalled.Should().BeTrue();
        _inboxRepositoryMock.Verify(r => r.TryRegisterAsync(It.IsAny<InboxEnvelope>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenResponseIsNotInboxProcessingResult_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var behavior = CreateBehavior<TestInvalidResponseInboxCommand, string>();
        var command = new TestInvalidResponseInboxCommand(CreateDefaultEnvelope());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("invalid");

        // Act
        var act = () => behavior.Handle(command, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Недопустимый тип ответа 'String'*");

        _inboxRepositoryMock.Verify(r => r.TryRegisterAsync(It.IsAny<InboxEnvelope>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMessageIsNew_ShouldRegister_AndReturnProcessed()
    {
        // Arrange
        var behavior = CreateBehavior<TestValidInboxCommand, InboxProcessingResult>();
        var envelope = CreateDefaultEnvelope();
        var command = new TestValidInboxCommand(envelope);

        _inboxRepositoryMock.Setup(r => r.TryRegisterAsync(envelope, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var nextCalled = false;
        RequestHandlerDelegate<InboxProcessingResult> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(InboxProcessingResult.Processed);
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(InboxProcessingResult.Processed);
        nextCalled.Should().BeTrue();
        _inboxRepositoryMock.Verify(r => r.TryRegisterAsync(envelope, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMessageIsDuplicate_ShouldReturnAlreadyProcessed_WithoutCallingNext()
    {
        // Arrange
        var behavior = CreateBehavior<TestValidInboxCommand, InboxProcessingResult>();
        var envelope = CreateDefaultEnvelope();
        var command = new TestValidInboxCommand(envelope);

        _inboxRepositoryMock.Setup(r => r.TryRegisterAsync(envelope, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Уже существует (дубликат)

        var nextCalled = false;
        RequestHandlerDelegate<InboxProcessingResult> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(InboxProcessingResult.Processed);
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(InboxProcessingResult.AlreadyProcessed);
        nextCalled.Should().BeFalse("Хендлер не должен выполняться для повторных дублирующих сообщений");
        _inboxRepositoryMock.Verify(r => r.TryRegisterAsync(envelope, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenHandlerReturnsUnexpectedResult_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var behavior = CreateBehavior<TestValidInboxCommand, InboxProcessingResult>();
        var envelope = CreateDefaultEnvelope();
        var command = new TestValidInboxCommand(envelope);

        _inboxRepositoryMock.Setup(r => r.TryRegisterAsync(envelope, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        RequestHandlerDelegate<InboxProcessingResult> next = _ => Task.FromResult(InboxProcessingResult.AlreadyProcessed);

        // Act
        var act = () => behavior.Handle(command, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Все успешные обработчики IInboxCommand обязаны возвращать Processed*");
    }

    private static InboxEnvelope CreateDefaultEnvelope() => new(
        Guid.NewGuid(), Guid.NewGuid(), null, "test-consumer", "test-topic", 0, 10,
        "key", "booking.cancelled.v1", "{}", "{}", DateTimeOffset.UtcNow);

    public sealed record TestRegularCommand : ICommand<string>;
    public sealed record TestValidInboxCommand(InboxEnvelope Inbox) : IInboxCommand;
    public sealed record TestInvalidResponseInboxCommand(InboxEnvelope Inbox) : ICommand<string>, IInboxCommand;
}
