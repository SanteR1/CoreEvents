using AwesomeAssertions;
using Events.Application.Abstractions.Repositories;
using Events.Application.Commands;
using Events.Application.Exceptions;
using Events.Domain.DomainEvents;
using Events.Domain.Entities;
using Events.Tests.Infrastructure;
using Moq;
using Xunit;

namespace Events.Tests.Commands;

public class CancelEventHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly CancelEventCommandHandler _handler;

    public CancelEventHandlerTests()
    {
        _handler = new CancelEventCommandHandler(_eventRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingId_ShouldDeactivateEventAndReturnTrue()
    {
        // Arrange
        var existEvent = TestEventFactory.Create();
        var command = new CancelEventCommand(existEvent.Id);

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existEvent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        existEvent.IsActive.Should().BeFalse();
        _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
        existEvent.PopDomainEvents().Should().ContainSingle(e => e is EventCancelledDomainEvent);
    }

    [Fact]
    public async Task Handle_WithInvalidId_ShouldThrowEventNotFoundException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var command = new CancelEventCommand(invalidId);

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        var exceptionAssertion = await act.Should().ThrowAsync<EventNotFoundException>();

        exceptionAssertion.Which.ErrorCode.Should().Be("Event.NotFound");
        exceptionAssertion.Which.ErrorData.Should().BeEquivalentTo(new { parameter = "Id", value = invalidId.ToString() });

        _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldPassTokenToRepository()
    {
        // Arrange
        var existEvent = TestEventFactory.Create();
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var command = new CancelEventCommand(existEvent.Id);

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existEvent);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _eventRepositoryMock.Verify(
            repo => repo.GetByIdAsync(existEvent.Id, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var existEvent = TestEventFactory.Create();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var cancellationToken = cts.Token;
        var command = new CancelEventCommand(existEvent.Id);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, cancellationToken);
        await act.Should().ThrowAsync<OperationCanceledException>();

        _eventRepositoryMock.Verify(
            repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAlreadyCancelled_ShouldBeIdempotentAndNotRaiseDuplicateEvent()
    {
        // Arrange
        var existEvent = TestEventFactory.Create();
        existEvent.Cancel(DateTimeOffset.UtcNow);
        existEvent.PopDomainEvents(); // Clear initial cancel event

        var command = new CancelEventCommand(existEvent.Id);

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existEvent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        existEvent.IsActive.Should().BeFalse();
        existEvent.PopDomainEvents().Should().BeEmpty();
    }
}

